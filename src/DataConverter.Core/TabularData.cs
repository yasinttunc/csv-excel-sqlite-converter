namespace DataConverter.Core;

/// <summary>
/// A rectangular table of text: a header row and data rows that all have one value per header.
/// Every format is read into this shape and written from it.
/// </summary>
public sealed class TabularData
{
    private TabularData(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        Headers = headers;
        Rows = rows;
    }

    public IReadOnlyList<string> Headers { get; }

    public IReadOnlyList<IReadOnlyList<string>> Rows { get; }

    public int ColumnCount => Headers.Count;

    public int RowCount => Rows.Count;

    /// <summary>
    /// Builds a table, cleaning up what real files contain: blank or repeated header names get unique names
    /// ("Column 3", "Name (2)"), short rows are padded with empty values, and long rows add extra columns.
    /// </summary>
    public static TabularData Create(IEnumerable<string?> headers, IEnumerable<IEnumerable<string?>> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        var headerList = headers.Select(h => (h ?? string.Empty).Trim()).ToList();
        var rowList = rows.Select(r => r.Select(v => v ?? string.Empty).ToList()).ToList();

        var width = Math.Max(headerList.Count, rowList.Count == 0 ? 0 : rowList.Max(r => r.Count));
        while (headerList.Count < width)
        {
            headerList.Add(string.Empty);
        }

        foreach (var row in rowList)
        {
            while (row.Count < width)
            {
                row.Add(string.Empty);
            }
        }

        return new TabularData(UniqueNames(headerList), rowList.Select(r => (IReadOnlyList<string>)r).ToList());
    }

    public int IndexOf(string header)
    {
        for (var i = 0; i < Headers.Count; i++)
        {
            if (string.Equals(Headers[i], header, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>The first <paramref name="count"/> rows, for previews.</summary>
    public TabularData Take(int count) => new(Headers, Rows.Take(count).ToList());

    private static List<string> UniqueNames(List<string> names)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(names.Count);
        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i].Length == 0 ? $"Column {i + 1}" : names[i];
            var candidate = name;
            for (var n = 2; !used.Add(candidate); n++)
            {
                candidate = $"{name} ({n})";
            }

            result.Add(candidate);
        }

        return result;
    }
}
