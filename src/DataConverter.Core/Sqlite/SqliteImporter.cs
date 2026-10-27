using Microsoft.Data.Sqlite;

namespace DataConverter.Core.Sqlite;

public enum IfTableExists
{
    /// <summary>Stop without changing anything.</summary>
    Fail,

    /// <summary>Drop the old table and create it again.</summary>
    Replace,

    /// <summary>Add the rows to the existing table (its columns must include every column being imported).</summary>
    Append,
}

/// <param name="TableName">Table to create. When splitting, used only for rows whose split value is empty.</param>
/// <param name="SplitByColumn">If set, one table is created per distinct value of this column, named after the value.</param>
public sealed record ImportOptions(
    string TableName,
    string? SplitByColumn = null,
    IfTableExists IfExists = IfTableExists.Fail,
    bool InferTypes = true);

public sealed record SqliteColumn(string Name, string Type);

public sealed record ImportedTable(string Name, int Rows, IReadOnlyList<SqliteColumn> Columns);

public sealed record ImportResult(string DatabasePath, IReadOnlyList<ImportedTable> Tables)
{
    public int TotalRows => Tables.Sum(t => t.Rows);
}

/// <summary>
/// Imports a table into a SQLite database file (created if missing). Everything happens in one transaction, every
/// value is bound as a parameter, and table and column names are quoted, so any header text is safe to use.
/// </summary>
public static class SqliteImporter
{
    public const int MaxTableNameLength = 128;

    public static ImportResult Import(TabularData data, string databasePath, ImportOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (data.ColumnCount == 0)
        {
            throw new DataFormatException("There is nothing to import: the file has no columns.");
        }

        var groups = Group(data, options);
        foreach (var name in groups.Keys)
        {
            ValidateTableName(name);
        }

        var columns = Enumerable.Range(0, data.ColumnCount)
            .Select(c => new SqliteColumn(
                data.Headers[c],
                options.InferTypes ? ColumnTypes.Infer(data.Rows.Select(r => r[c])) : ColumnTypes.Text))
            .ToList();

        using var connection = SqliteDatabase.Open(databasePath, readOnly: false);
        using var transaction = connection.BeginTransaction();

        var existing = SqliteDatabase.TableNames(connection, transaction).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var clashes = groups.Keys.Where(existing.Contains).ToList();
        if (options.IfExists == IfTableExists.Fail && clashes.Count > 0)
        {
            throw new DataFormatException(
                $"The database already has table(s) {string.Join(", ", clashes.Select(c => $"\"{c}\""))}. Choose Replace or Append.");
        }

        var imported = new List<ImportedTable>();
        foreach (var (table, rows) in groups)
        {
            var exists = existing.Contains(table);
            if (exists && options.IfExists == IfTableExists.Replace)
            {
                Execute(connection, transaction, $"DROP TABLE {Quote(table)}");
                exists = false;
            }

            if (exists)
            {
                var tableColumns = SqliteDatabase.ColumnNames(connection, transaction, table);
                var missing = columns.Where(c => !tableColumns.Contains(c.Name, StringComparer.OrdinalIgnoreCase)).Select(c => c.Name).ToList();
                if (missing.Count > 0)
                {
                    throw new DataFormatException(
                        $"Table \"{table}\" has no column(s) {string.Join(", ", missing.Select(m => $"\"{m}\""))}, so the rows cannot be appended.");
                }
            }
            else
            {
                Execute(connection, transaction,
                    $"CREATE TABLE {Quote(table)} ({string.Join(", ", columns.Select(c => $"{Quote(c.Name)} {c.Type}"))})");
            }

            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                $"INSERT INTO {Quote(table)} ({string.Join(", ", columns.Select(c => Quote(c.Name)))}) " +
                $"VALUES ({string.Join(", ", columns.Select((_, i) => $"@p{i}"))})";
            for (var i = 0; i < columns.Count; i++)
            {
                insert.Parameters.Add(new SqliteParameter($"@p{i}", DBNull.Value));
            }

            foreach (var row in rows)
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    insert.Parameters[i].Value = ColumnTypes.ToDbValue(row[i], columns[i].Type);
                }

                insert.ExecuteNonQuery();
            }

            imported.Add(new ImportedTable(table, rows.Count, columns));
        }

        transaction.Commit();
        return new ImportResult(Path.GetFullPath(databasePath), imported);
    }

    /// <summary>Quotes a table or column name for SQL ("a""b" for a"b), so any text can be used as a name.</summary>
    public static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    public static void ValidateTableName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DataFormatException("Enter a table name.");
        }

        if (name.Length > MaxTableNameLength)
        {
            throw new DataFormatException($"Table names can be at most {MaxTableNameLength} characters: \"{name[..20]}...\".");
        }

        if (name.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase))
        {
            throw new DataFormatException($"\"{name}\" cannot be used: names starting with sqlite_ are reserved by SQLite.");
        }
    }

    private static Dictionary<string, List<IReadOnlyList<string>>> Group(TabularData data, ImportOptions options)
    {
        var groups = new Dictionary<string, List<IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(options.SplitByColumn))
        {
            groups[options.TableName.Trim()] = data.Rows.ToList();
            return groups;
        }

        var index = data.IndexOf(options.SplitByColumn);
        if (index < 0)
        {
            throw new DataFormatException(
                $"There is no column \"{options.SplitByColumn}\" to split by. Columns: {string.Join(", ", data.Headers)}.");
        }

        foreach (var row in data.Rows)
        {
            var key = row[index].Trim();
            if (key.Length == 0)
            {
                key = options.TableName.Trim();
            }

            if (!groups.TryGetValue(key, out var list))
            {
                groups[key] = list = [];
            }

            list.Add(row);
        }

        return groups;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
