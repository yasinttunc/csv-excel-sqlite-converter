using System.Text;
using DataConverter.Core.Csv;
using DataConverter.Core.Excel;

namespace DataConverter.Core;

/// <summary>Kinds of file the converter understands, chosen by extension.</summary>
public enum DataFileKind
{
    Csv,
    Excel,
    Sqlite,
}

/// <summary>Opens and saves tables by file extension: .csv/.tsv/.txt, .xlsx/.xlsm, or .db/.sqlite/.sqlite3.</summary>
public static class DataFiles
{
    public static DataFileKind KindOf(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".csv" or ".tsv" or ".txt" => DataFileKind.Csv,
            ".xlsx" or ".xlsm" => DataFileKind.Excel,
            ".db" or ".sqlite" or ".sqlite3" => DataFileKind.Sqlite,
            ".xls" => throw new DataFormatException("Old .xls workbooks are not supported. Open the file in Excel and save it as .xlsx."),
            _ => throw new DataFormatException($"Unsupported file type \"{extension}\". Use .csv, .xlsx or a SQLite .db file."),
        };
    }

    /// <summary>Reads a CSV file (delimiter and encoding detected unless given) or one sheet of a workbook.</summary>
    public static TabularData Load(string path, string? sheetName = null, char? delimiter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new DataFormatException($"The file \"{path}\" does not exist.");
        }

        return KindOf(path) switch
        {
            DataFileKind.Csv => CsvFormat.Parse(TextFiles.Read(path, out _), delimiter ?? (Path.GetExtension(path).Equals(".tsv", StringComparison.OrdinalIgnoreCase) ? (char?)'\t' : null)),
            DataFileKind.Excel => ExcelFiles.ReadSheet(path, sheetName),
            _ => throw new DataFormatException("Open a SQLite database with the Browse view or the \"tables\" command."),
        };
    }

    /// <summary>Saves a table as CSV (UTF-8 with BOM, so Excel shows non-English letters correctly) or as .xlsx.</summary>
    public static void Save(TabularData data, string path, char delimiter = ',', string sheetName = "Sheet1")
    {
        ArgumentNullException.ThrowIfNull(data);
        switch (KindOf(path))
        {
            case DataFileKind.Csv:
                File.WriteAllText(path, CsvFormat.Write(data, delimiter), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                break;
            case DataFileKind.Excel:
                ExcelFiles.Write(data, path, sheetName);
                break;
            default:
                throw new DataFormatException("Use the import command to write to a SQLite database.");
        }
    }
}
