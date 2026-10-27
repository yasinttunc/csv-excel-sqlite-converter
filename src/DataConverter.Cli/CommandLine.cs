using System.Globalization;
using System.Reflection;
using DataConverter.Core;
using DataConverter.Core.Excel;
using DataConverter.Core.Sqlite;

namespace DataConverter.Cli;

/// <summary>The <c>dataconv</c> command-line tool. Exit codes: 0 success, 1 the data could not be converted, 2 wrong usage.</summary>
public static class CommandLine
{
    public const int Ok = 0;
    public const int DataError = 1;
    public const int UsageError = 2;

    public const string Usage = """
        dataconv: convert between CSV, Excel (.xlsx) and SQLite.

        Usage:
          dataconv convert <input.csv|.xlsx> <output.csv|.xlsx> [--sheet NAME] [--delimiter D] [--out-delimiter D]
          dataconv import  <input.csv|.xlsx> <database.db> [--table NAME] [--split-by COLUMN]
                           [--if-exists fail|replace|append] [--sheet NAME] [--delimiter D] [--text-only]
          dataconv tables  <database.db>
          dataconv export  <database.db> <table> <output.csv|.xlsx>
          dataconv sheets  <workbook.xlsx>
          dataconv preview <input.csv|.xlsx> [--rows N] [--sheet NAME] [--delimiter D]

        Delimiters: , ; tab pipe (detected automatically when not given).
        --split-by creates one table per value of COLUMN, named after the value.
        --text-only stores every column as TEXT instead of detecting INTEGER and REAL columns.
        """;

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length == 0 || args[0] is "help" or "-h" or "--help")
        {
            output.WriteLine(Usage);
            return args.Length == 0 ? UsageError : Ok;
        }

        if (args[0] is "--version" or "version")
        {
            output.WriteLine($"dataconv {typeof(CommandLine).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?"}");
            return Ok;
        }

        try
        {
            var parsed = Arguments.Parse(args.Skip(1));
            return args[0] switch
            {
                "convert" => Convert(parsed, output),
                "import" => Import(parsed, output),
                "tables" => Tables(parsed, output),
                "export" => Export(parsed, output),
                "sheets" => Sheets(parsed, output),
                "preview" => Preview(parsed, output),
                _ => throw new UsageException($"Unknown command \"{args[0]}\"."),
            };
        }
        catch (UsageException ex)
        {
            error.WriteLine($"error: {ex.Message}");
            error.WriteLine("Run \"dataconv help\" for usage.");
            return UsageError;
        }
        catch (DataFormatException ex)
        {
            error.WriteLine($"error: {ex.Message}");
            return DataError;
        }
        catch (IOException ex)
        {
            error.WriteLine($"error: {ex.Message}");
            return DataError;
        }
        catch (UnauthorizedAccessException ex)
        {
            error.WriteLine($"error: {ex.Message}");
            return DataError;
        }
    }

    private static int Convert(Arguments a, TextWriter output)
    {
        a.ExpectPositional(2, "convert <input> <output>");
        a.AllowOptions("sheet", "delimiter", "out-delimiter");
        var data = DataFiles.Load(a.Positional[0], a.Option("sheet"), a.Delimiter("delimiter"));
        DataFiles.Save(data, a.Positional[1], a.Delimiter("out-delimiter") ?? ',', SheetNameFor(a.Positional[1]));
        output.WriteLine($"Wrote {data.RowCount} rows and {data.ColumnCount} columns to {a.Positional[1]}.");
        return Ok;
    }

    private static int Import(Arguments a, TextWriter output)
    {
        a.ExpectPositional(2, "import <input> <database>");
        a.AllowOptions("table", "split-by", "if-exists", "sheet", "delimiter", "text-only");
        var ifExists = a.Option("if-exists") switch
        {
            null or "fail" => IfTableExists.Fail,
            "replace" => IfTableExists.Replace,
            "append" => IfTableExists.Append,
            var other => throw new UsageException($"--if-exists must be fail, replace or append, not \"{other}\"."),
        };
        var data = DataFiles.Load(a.Positional[0], a.Option("sheet"), a.Delimiter("delimiter"));

        var options = new ImportOptions(
            a.Option("table") ?? Path.GetFileNameWithoutExtension(a.Positional[0]),
            a.Option("split-by"),
            ifExists,
            InferTypes: !a.Flag("text-only"));
        var result = SqliteImporter.Import(data, a.Positional[1], options);

        output.WriteLine($"Imported {result.TotalRows} rows into {result.Tables.Count} table(s) in {a.Positional[1]}:");
        foreach (var table in result.Tables)
        {
            output.WriteLine($"  {table.Name,-30} {table.Rows,6} rows");
        }

        if (result.Tables.Count > 0)
        {
            output.WriteLine("Columns: " + string.Join(", ", result.Tables[0].Columns.Select(c => $"{c.Name} {c.Type}")));
        }

        return Ok;
    }

    private static int Tables(Arguments a, TextWriter output)
    {
        a.ExpectPositional(1, "tables <database>");
        a.AllowOptions();
        var tables = SqliteDatabase.ListTables(a.Positional[0]);
        if (tables.Count == 0)
        {
            output.WriteLine("The database has no tables.");
        }

        foreach (var table in tables)
        {
            output.WriteLine($"{table.Name,-30} {table.Rows,8} rows");
        }

        return Ok;
    }

    private static int Export(Arguments a, TextWriter output)
    {
        a.ExpectPositional(3, "export <database> <table> <output>");
        a.AllowOptions("out-delimiter");
        var data = SqliteDatabase.ReadTable(a.Positional[0], a.Positional[1]);
        DataFiles.Save(data, a.Positional[2], a.Delimiter("out-delimiter") ?? ',', a.Positional[1]);
        output.WriteLine($"Wrote {data.RowCount} rows from \"{a.Positional[1]}\" to {a.Positional[2]}.");
        return Ok;
    }

    private static int Sheets(Arguments a, TextWriter output)
    {
        a.ExpectPositional(1, "sheets <workbook>");
        a.AllowOptions();
        foreach (var name in ExcelFiles.SheetNames(a.Positional[0]))
        {
            output.WriteLine(name);
        }

        return Ok;
    }

    private static int Preview(Arguments a, TextWriter output)
    {
        a.ExpectPositional(1, "preview <input>");
        a.AllowOptions("rows", "sheet", "delimiter");
        var rows = 10;
        if (a.Option("rows") is { } text && (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out rows) || rows < 1))
        {
            throw new UsageException("--rows must be a positive whole number.");
        }

        var data = DataFiles.Load(a.Positional[0], a.Option("sheet"), a.Delimiter("delimiter"));
        output.Write(TextTable.Render(data.Take(rows)));
        output.WriteLine($"{data.RowCount} rows, {data.ColumnCount} columns.");
        return Ok;
    }

    private static string SheetNameFor(string path) => Path.GetFileNameWithoutExtension(path);
}

/// <summary>Wrong command-line usage; reported with exit code 2.</summary>
public sealed class UsageException(string message) : Exception(message);

/// <summary>Positional arguments and --name value options. Flags (--text-only) take no value.</summary>
public sealed class Arguments
{
    private static readonly HashSet<string> Flags = ["text-only"];
    private readonly Dictionary<string, string?> _options = new(StringComparer.Ordinal);

    public List<string> Positional { get; } = [];

    public static Arguments Parse(IEnumerable<string> args)
    {
        var result = new Arguments();
        using var e = args.GetEnumerator();
        while (e.MoveNext())
        {
            var arg = e.Current;
            if (arg.StartsWith("--", StringComparison.Ordinal) && arg.Length > 2)
            {
                var name = arg[2..];
                if (Flags.Contains(name))
                {
                    result._options[name] = null;
                    continue;
                }

                if (!e.MoveNext())
                {
                    throw new UsageException($"--{name} needs a value.");
                }

                result._options[name] = e.Current;
            }
            else
            {
                result.Positional.Add(arg);
            }
        }

        return result;
    }

    public string? Option(string name) => _options.TryGetValue(name, out var value) ? value : null;

    public bool Flag(string name) => _options.ContainsKey(name);

    public char? Delimiter(string name) => Option(name) switch
    {
        null => null,
        "," or "comma" => ',',
        ";" or "semicolon" => ';',
        "tab" or "\\t" or "\t" => '\t',
        "|" or "pipe" => '|',
        var other => throw new UsageException($"--{name} must be one of , ; tab pipe, not \"{other}\"."),
    };

    public void ExpectPositional(int count, string usage)
    {
        if (Positional.Count != count)
        {
            throw new UsageException($"Expected: dataconv {usage}");
        }
    }

    public void AllowOptions(params string[] allowed)
    {
        var unknown = _options.Keys.Where(k => !allowed.Contains(k)).ToList();
        if (unknown.Count > 0)
        {
            throw new UsageException($"Unknown option(s): {string.Join(", ", unknown.Select(u => "--" + u))}.");
        }
    }
}
