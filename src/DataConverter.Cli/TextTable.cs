using System.Text;
using DataConverter.Core;

namespace DataConverter.Cli;

/// <summary>Draws a table as aligned plain text for the terminal.</summary>
public static class TextTable
{
    public const int MaxColumnWidth = 24;

    public static string Render(TabularData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var widths = Enumerable.Range(0, data.ColumnCount)
            .Select(c => Math.Min(MaxColumnWidth, data.Rows.Select(r => r[c].Length).Append(data.Headers[c].Length).Max()))
            .ToArray();

        var text = new StringBuilder();
        void Line(IReadOnlyList<string> values)
        {
            text.AppendLine(string.Join(" | ", values.Select((v, i) => Fit(v, widths[i]))).TrimEnd());
        }

        Line(data.Headers);
        text.AppendLine(string.Join("-+-", widths.Select(w => new string('-', w))));
        foreach (var row in data.Rows)
        {
            Line(row);
        }

        return text.ToString();
    }

    private static string Fit(string value, int width)
    {
        var single = value.Replace("\r", " ").Replace("\n", " ");
        return single.Length > width ? single[..(width - 1)] + "…" : single.PadRight(width);
    }
}
