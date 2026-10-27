using System.Globalization;
using System.Text.RegularExpressions;

namespace DataConverter.Core.Sqlite;

/// <summary>Chooses a SQLite column type from the values in a column.</summary>
public static partial class ColumnTypes
{
    public const string Integer = "INTEGER";
    public const string Real = "REAL";
    public const string Text = "TEXT";

    /// <summary>
    /// INTEGER if every non-empty value is a whole number, REAL if every non-empty value is a number, otherwise TEXT.
    /// Values with leading zeros ("007", "0555 123") make the column TEXT so they are stored exactly as written.
    /// </summary>
    public static string Infer(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var type = Integer;
        var any = false;

        foreach (var value in values)
        {
            if (value.Length == 0)
            {
                continue;
            }

            any = true;
            if (type == Integer && !IsInteger(value))
            {
                type = Real;
            }

            if (type == Real && !IsReal(value))
            {
                return Text;
            }
        }

        return any ? type : Text;
    }

    public static bool IsInteger(string value) => IntegerPattern().IsMatch(value) && long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);

    public static bool IsReal(string value) =>
        RealPattern().IsMatch(value)
        && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
        && double.IsFinite(number);

    /// <summary>Converts the text to the value stored for a column of the given type; empty text is NULL.</summary>
    public static object ToDbValue(string value, string type) => value.Length == 0
        ? DBNull.Value
        : type switch
        {
            Integer => long.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
            Real => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture),
            _ => value,
        };

    [GeneratedRegex(@"^-?(0|[1-9][0-9]{0,18})$")]
    private static partial Regex IntegerPattern();

    [GeneratedRegex(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][-+]?[0-9]+)?$")]
    private static partial Regex RealPattern();
}
