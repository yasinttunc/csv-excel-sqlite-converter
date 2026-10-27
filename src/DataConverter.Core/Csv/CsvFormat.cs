using System.Text;

namespace DataConverter.Core.Csv;

/// <summary>
/// Reads and writes CSV as described in RFC 4180: fields may be quoted, quotes inside a quoted field are doubled,
/// and quoted fields may contain the delimiter and line breaks. The delimiter can be detected, because spreadsheets
/// in many locales (Turkish and German Excel, for example) save CSV with semicolons.
/// </summary>
public static class CsvFormat
{
    public static IReadOnlyList<char> CandidateDelimiters { get; } = [',', ';', '\t', '|'];

    /// <summary>
    /// Picks the delimiter that splits the first lines into the same number of fields (more than one), preferring the
    /// one that gives the most fields. Characters inside quotes are ignored. Falls back to a comma.
    /// </summary>
    public static char DetectDelimiter(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var best = ',';
        var bestFields = 1;

        foreach (var candidate in CandidateDelimiters)
        {
            var records = ParseRecords(text, candidate, maxRecords: 10).Where(r => !IsBlank(r)).ToList();
            if (records.Count == 0)
            {
                continue;
            }

            var fields = records[0].Count;
            if (fields > bestFields && records.All(r => r.Count == fields))
            {
                best = candidate;
                bestFields = fields;
            }
        }

        if (bestFields == 1)
        {
            // Ragged files: choose the candidate that appears most often in the first line.
            var firstLine = text.Split('\n', 2)[0];
            var (delimiter, count) = CandidateDelimiters.Select(d => (d, firstLine.Count(c => c == d))).MaxBy(x => x.Item2);
            if (count > 0)
            {
                best = delimiter;
            }
        }

        return best;
    }

    /// <summary>Parses CSV text. The first record is the header unless <paramref name="hasHeader"/> is false.</summary>
    public static TabularData Parse(string text, char? delimiter = null, bool hasHeader = true)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > 0 && text[0] == '﻿')
        {
            text = text[1..];
        }

        var separator = delimiter ?? DetectDelimiter(text);
        var records = ParseRecords(text, separator).Where(r => !IsBlank(r)).ToList();
        if (records.Count == 0)
        {
            return TabularData.Create([], []);
        }

        return hasHeader
            ? TabularData.Create(records[0], records.Skip(1))
            : TabularData.Create([], records);
    }

    /// <summary>Writes CSV with a header row, quoting only the fields that need it. Line breaks are CRLF.</summary>
    public static string Write(TabularData data, char delimiter = ',')
    {
        ArgumentNullException.ThrowIfNull(data);
        var csv = new StringBuilder();
        AppendRecord(csv, data.Headers, delimiter);
        foreach (var row in data.Rows)
        {
            AppendRecord(csv, row, delimiter);
        }

        return csv.ToString();
    }

    internal static List<List<string>> ParseRecords(string text, char delimiter, int maxRecords = int.MaxValue)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var fieldStarted = false;

        for (var i = 0; i < text.Length && records.Count < maxRecords; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            if (c == '"' && !fieldStarted)
            {
                inQuotes = true;
                fieldStarted = true;
            }
            else if (c == delimiter)
            {
                record.Add(field.ToString());
                field.Clear();
                fieldStarted = false;
            }
            else if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                record.Add(field.ToString());
                records.Add(record);
                record = [];
                field.Clear();
                fieldStarted = false;
            }
            else
            {
                field.Append(c);
                fieldStarted = true;
            }
        }

        if ((fieldStarted || field.Length > 0 || record.Count > 0) && records.Count < maxRecords)
        {
            record.Add(field.ToString());
            records.Add(record);
        }

        return records;
    }

    private static bool IsBlank(List<string> record) => record.All(string.IsNullOrWhiteSpace);

    private static void AppendRecord(StringBuilder csv, IEnumerable<string> fields, char delimiter)
    {
        var first = true;
        foreach (var value in fields)
        {
            if (!first)
            {
                csv.Append(delimiter);
            }

            first = false;
            var needsQuotes = value.Contains(delimiter) || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
                || (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1])));
            csv.Append(needsQuotes ? "\"" + value.Replace("\"", "\"\"") + "\"" : value);
        }

        csv.Append("\r\n");
    }
}
