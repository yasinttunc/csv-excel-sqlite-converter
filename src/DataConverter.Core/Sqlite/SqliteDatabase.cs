using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DataConverter.Core.Sqlite;

public sealed record TableInfo(string Name, long Rows);

/// <summary>Lists and reads the tables of a SQLite database file.</summary>
public static class SqliteDatabase
{
    public static IReadOnlyList<TableInfo> ListTables(string databasePath)
    {
        using var connection = Open(databasePath, readOnly: true);
        return TableNames(connection, null)
            .Select(name =>
            {
                using var count = connection.CreateCommand();
                count.CommandText = $"SELECT count(*) FROM {SqliteImporter.Quote(name)}";
                return new TableInfo(name, (long)count.ExecuteScalar()!);
            })
            .ToList();
    }

    /// <summary>Reads a table (at most <paramref name="limit"/> rows) as text. Only existing table names are accepted.</summary>
    public static TabularData ReadTable(string databasePath, string table, int? limit = null)
    {
        using var connection = Open(databasePath, readOnly: true);
        var name = TableNames(connection, null).FirstOrDefault(t => string.Equals(t, table, StringComparison.OrdinalIgnoreCase))
            ?? throw new DataFormatException($"The database has no table named \"{table}\".");

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {SqliteImporter.Quote(name)} LIMIT @limit";
        command.Parameters.AddWithValue("@limit", limit ?? -1);

        using var reader = command.ExecuteReader();
        var headers = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        var rows = new List<List<string>>();
        while (reader.Read())
        {
            rows.Add(Enumerable.Range(0, reader.FieldCount).Select(i => Format(reader.GetValue(i))).ToList());
        }

        return TabularData.Create(headers, rows);
    }

    internal static SqliteConnection Open(string databasePath, bool readOnly)
    {
        if (readOnly && !File.Exists(databasePath))
        {
            throw new DataFormatException($"The database file \"{databasePath}\" does not exist.");
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!readOnly && !string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString());
        try
        {
            connection.Open();
            using var check = connection.CreateCommand();
            check.CommandText = "SELECT count(*) FROM sqlite_master";
            check.ExecuteScalar();
        }
        catch (SqliteException ex)
        {
            connection.Dispose();
            throw new DataFormatException($"\"{Path.GetFileName(databasePath)}\" is not a SQLite database ({ex.Message}).");
        }

        return connection;
    }

    internal static List<string> TableNames(SqliteConnection connection, SqliteTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' ORDER BY name";
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    internal static List<string> ColumnNames(SqliteConnection connection, SqliteTransaction? transaction, string table)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT name FROM pragma_table_info(@table)";
        command.Parameters.AddWithValue("@table", table);
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static string Format(object value) => value switch
    {
        DBNull => string.Empty,
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        long l => l.ToString(CultureInfo.InvariantCulture),
        byte[] bytes => $"[{bytes.Length} bytes]",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };
}
