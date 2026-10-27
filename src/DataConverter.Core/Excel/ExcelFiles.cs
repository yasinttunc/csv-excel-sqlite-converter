using System.Globalization;
using ClosedXML.Excel;

namespace DataConverter.Core.Excel;

/// <summary>Reads and writes .xlsx workbooks with ClosedXML.</summary>
public static class ExcelFiles
{
    public const int MaxSheetNameLength = 31;

    private static readonly char[] InvalidSheetNameChars = ['[', ']', ':', '*', '?', '/', '\\'];

    public static IReadOnlyList<string> SheetNames(Stream workbook)
    {
        using var book = Open(workbook);
        return book.Worksheets.Select(w => w.Name).ToList();
    }

    public static IReadOnlyList<string> SheetNames(string path)
    {
        using var stream = File.OpenRead(path);
        return SheetNames(stream);
    }

    /// <summary>
    /// Reads one sheet (the first if <paramref name="sheetName"/> is null). The first used row is the header.
    /// Merged cells are refused, because a merged value belongs to several rows or columns at once.
    /// </summary>
    public static TabularData ReadSheet(Stream workbook, string? sheetName = null)
    {
        using var book = Open(workbook);
        var sheet = sheetName is null
            ? book.Worksheets.FirstOrDefault()
            : book.Worksheets.FirstOrDefault(w => string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase));
        if (sheet is null)
        {
            throw new DataFormatException(sheetName is null
                ? "The workbook has no sheets."
                : $"The workbook has no sheet named \"{sheetName}\". Sheets: {string.Join(", ", book.Worksheets.Select(w => w.Name))}.");
        }

        var merged = sheet.MergedRanges.Select(r => r.RangeAddress.ToString()).ToList();
        if (merged.Count > 0)
        {
            throw new DataFormatException(
                $"Sheet \"{sheet.Name}\" has merged cells ({string.Join(", ", merged.Take(5))}). Unmerge them and try again.");
        }

        var used = sheet.RangeUsed();
        if (used is null)
        {
            return TabularData.Create([], []);
        }

        var firstRow = used.FirstRow().RowNumber();
        var lastRow = used.LastRow().RowNumber();
        var firstColumn = used.FirstColumn().ColumnNumber();
        var lastColumn = used.LastColumn().ColumnNumber();

        List<string> ReadRow(int row) =>
            Enumerable.Range(firstColumn, lastColumn - firstColumn + 1).Select(c => Text(sheet.Cell(row, c))).ToList();

        var header = ReadRow(firstRow);
        var rows = Enumerable.Range(firstRow + 1, Math.Max(0, lastRow - firstRow))
            .Select(ReadRow)
            .Where(r => r.Any(v => v.Length > 0))
            .ToList();
        return TabularData.Create(header, rows);
    }

    public static TabularData ReadSheet(string path, string? sheetName = null)
    {
        using var stream = File.OpenRead(path);
        return ReadSheet(stream, sheetName);
    }

    /// <summary>
    /// Writes the table to one sheet with a bold, frozen header row and a filter. Values that are plain numbers
    /// become numeric cells; values with leading zeros (student numbers, phone numbers) stay text so nothing is lost.
    /// </summary>
    public static void Write(TabularData data, Stream output, string sheetName = "Sheet1")
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(output);

        using var book = new XLWorkbook();
        var sheet = book.AddWorksheet(SafeSheetName(sheetName));

        for (var c = 0; c < data.ColumnCount; c++)
        {
            sheet.Cell(1, c + 1).Value = data.Headers[c];
        }

        for (var r = 0; r < data.RowCount; r++)
        {
            var row = data.Rows[r];
            for (var c = 0; c < data.ColumnCount; c++)
            {
                var cell = sheet.Cell(r + 2, c + 1);
                if (TryNumber(row[c], out var number))
                {
                    cell.Value = number;
                }
                else if (row[c].Length > 0)
                {
                    cell.Value = row[c];
                }
            }
        }

        if (data.ColumnCount > 0)
        {
            sheet.Row(1).Style.Font.Bold = true;
            sheet.SheetView.FreezeRows(1);
            sheet.Range(1, 1, data.RowCount + 1, data.ColumnCount).SetAutoFilter();
            for (var c = 0; c < data.ColumnCount; c++)
            {
                var longest = data.Rows.Select(r => r[c].Length).Append(data.Headers[c].Length).Max();
                sheet.Column(c + 1).Width = Math.Clamp(longest + 3, 8, 60);
            }
        }

        book.SaveAs(output);
    }

    public static void Write(TabularData data, string path, string sheetName = "Sheet1")
    {
        using var stream = File.Create(path);
        Write(data, stream, sheetName);
    }

    /// <summary>Makes a valid sheet name: no []:*?/\ characters, not empty, at most 31 characters.</summary>
    public static string SafeSheetName(string? name)
    {
        var cleaned = new string((name ?? string.Empty).Select(ch => InvalidSheetNameChars.Contains(ch) ? '_' : ch).ToArray()).Trim().Trim('\'');
        if (cleaned.Length == 0)
        {
            cleaned = "Sheet1";
        }

        return cleaned.Length > MaxSheetNameLength ? cleaned[..MaxSheetNameLength] : cleaned;
    }

    /// <summary>True for plain numbers such as 42, -3.5 or 1e3; false for "007", "1,5" or values over 15 digits.</summary>
    public static bool TryNumber(string text, out double number)
    {
        number = 0;
        if (text.Length == 0 || text.Length > 15 || char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]))
        {
            return false;
        }

        var digits = text.StartsWith('-') ? text[1..] : text;
        if (digits.Length > 1 && digits[0] == '0' && digits[1] != '.')
        {
            return false;
        }

        return double.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture, out number) && double.IsFinite(number);
    }

    private static XLWorkbook Open(Stream stream)
    {
        try
        {
            return new XLWorkbook(stream);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not DataFormatException)
        {
            throw new DataFormatException($"The file could not be read as an Excel workbook (.xlsx): {ex.Message}");
        }
    }

    private static string Text(IXLCell cell)
    {
        var value = cell.Value;
        if (value.IsBlank)
        {
            return string.Empty;
        }

        if (value.IsNumber)
        {
            return value.GetNumber().ToString("0.###############", CultureInfo.InvariantCulture);
        }

        if (value.IsDateTime)
        {
            var date = value.GetDateTime();
            return date.TimeOfDay == TimeSpan.Zero
                ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        if (value.IsBoolean)
        {
            return value.GetBoolean() ? "TRUE" : "FALSE";
        }

        if (value.IsText)
        {
            return value.GetText().Trim();
        }

        return cell.GetFormattedString().Trim();
    }
}
