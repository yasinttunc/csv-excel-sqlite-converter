using DataConverter.Core.Sqlite;
using Microsoft.Data.Sqlite;

namespace DataConverter.Core.Tests;

public class SqliteTests : IDisposable
{
    private readonly TempFolder _temp = new();

    public void Dispose() => _temp.Dispose();

    private string Db => _temp.File("test.db");

    private static TabularData Grades() => TabularData.Create(
        ["Number", "Name", "Section", "Score", "Average"],
        [
            ["00123", "Şeyma Kaya", "TKM405-41", "87", "73.5"],
            ["00124", "Ali Yılmaz", "TKM405-42", "", "61"],
            ["00125", "Ece Taş", "TKM405-41", "55", "40.25"],
        ]);

    [Theory]
    [InlineData(new[] { "1", "-20", "" }, "INTEGER")]
    [InlineData(new[] { "1", "2.5" }, "REAL")]
    [InlineData(new[] { "1e3", "-0.5" }, "REAL")]
    [InlineData(new[] { "007", "8" }, "TEXT")]
    [InlineData(new[] { "1,5" }, "TEXT")]
    [InlineData(new[] { "", "" }, "TEXT")]
    [InlineData(new[] { "99999999999999999999" }, "REAL")]
    public void Infer_ChoosesTheNarrowestSafeType(string[] values, string expected)
    {
        Assert.Equal(expected, ColumnTypes.Infer(values));
    }

    [Fact]
    public void Import_CreatesOneTypedTable()
    {
        var result = SqliteImporter.Import(Grades(), Db, new ImportOptions("grades"));

        var table = Assert.Single(result.Tables);
        Assert.Equal(3, table.Rows);
        Assert.Equal(new[] { "TEXT", "TEXT", "TEXT", "INTEGER", "REAL" }, table.Columns.Select(c => c.Type));
        Assert.Equal("grades", Assert.Single(SqliteDatabase.ListTables(Db)).Name);
        Assert.Equal(DBNull.Value, Scalar("SELECT Score FROM grades WHERE Number = '00124'"));
        Assert.Equal(73.5, Scalar("SELECT Average FROM grades WHERE Number = '00123'"));
    }

    [Fact]
    public void Import_SplitByColumn_MakesOneTablePerValue()
    {
        var result = SqliteImporter.Import(Grades(), Db, new ImportOptions("grades", SplitByColumn: "section"));

        Assert.Equal(new[] { "TKM405-41", "TKM405-42" }, result.Tables.Select(t => t.Name));
        Assert.Equal(2, result.Tables[0].Rows);
        Assert.Equal(new[] { 2L, 1L }, SqliteDatabase.ListTables(Db).Select(t => t.Rows));
    }

    [Fact]
    public void Import_SplitByMissingColumn_IsReported()
    {
        var error = Assert.Throws<DataFormatException>(() => SqliteImporter.Import(Grades(), Db, new ImportOptions("t", SplitByColumn: "Class")));
        Assert.Contains("Class", error.Message);
    }

    [Fact]
    public void Import_ExistingTable_FailReplaceAppend()
    {
        SqliteImporter.Import(Grades(), Db, new ImportOptions("grades"));

        Assert.Throws<DataFormatException>(() => SqliteImporter.Import(Grades(), Db, new ImportOptions("GRADES")));
        Assert.Equal(3L, SqliteDatabase.ListTables(Db)[0].Rows);

        SqliteImporter.Import(Grades(), Db, new ImportOptions("grades", IfExists: IfTableExists.Append));
        Assert.Equal(6L, SqliteDatabase.ListTables(Db)[0].Rows);

        SqliteImporter.Import(Grades(), Db, new ImportOptions("grades", IfExists: IfTableExists.Replace));
        Assert.Equal(3L, SqliteDatabase.ListTables(Db)[0].Rows);
    }

    [Fact]
    public void Import_AppendWithDifferentColumns_ChangesNothing()
    {
        SqliteImporter.Import(TabularData.Create(["Other"], [["x"]]), Db, new ImportOptions("grades"));

        Assert.Throws<DataFormatException>(() => SqliteImporter.Import(Grades(), Db, new ImportOptions("grades", IfExists: IfTableExists.Append)));
        Assert.Equal(1L, SqliteDatabase.ListTables(Db)[0].Rows);
    }

    [Fact]
    public void Import_QuotesHostileNames()
    {
        var data = TabularData.Create(["a\"b", "x); DROP TABLE t; --"], [["1", "2"]]);

        SqliteImporter.Import(data, Db, new ImportOptions("weird \"name\"; --"));

        var table = Assert.Single(SqliteDatabase.ListTables(Db));
        Assert.Equal("weird \"name\"; --", table.Name);
        Assert.Equal(new[] { "a\"b", "x); DROP TABLE t; --" }, SqliteDatabase.ReadTable(Db, table.Name).Headers);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sqlite_master")]
    public void Import_InvalidTableName_IsRefused(string name)
    {
        Assert.Throws<DataFormatException>(() => SqliteImporter.Import(Grades(), Db, new ImportOptions(name)));
    }

    [Fact]
    public void Import_TextOnly_StoresEverythingAsText()
    {
        var result = SqliteImporter.Import(Grades(), Db, new ImportOptions("grades", InferTypes: false));

        Assert.All(result.Tables[0].Columns, c => Assert.Equal("TEXT", c.Type));
    }

    [Fact]
    public void ReadTable_ReturnsTextAndHonoursTheLimit()
    {
        SqliteImporter.Import(Grades(), Db, new ImportOptions("grades"));

        var all = SqliteDatabase.ReadTable(Db, "Grades");
        var first = SqliteDatabase.ReadTable(Db, "grades", limit: 1);

        Assert.Equal(3, all.RowCount);
        Assert.Equal(new[] { "00124", "Ali Yılmaz", "TKM405-42", "", "61" }, all.Rows[1]);
        Assert.Equal(1, first.RowCount);
    }

    [Fact]
    public void ReadTable_UnknownTable_OrMissingFile_IsReported()
    {
        SqliteImporter.Import(Grades(), Db, new ImportOptions("grades"));

        Assert.Throws<DataFormatException>(() => SqliteDatabase.ReadTable(Db, "nope"));
        Assert.Throws<DataFormatException>(() => SqliteDatabase.ListTables(_temp.File("missing.db")));
    }

    [Fact]
    public void ListTables_NotADatabase_IsReported()
    {
        File.WriteAllText(_temp.File("fake.db"), "this is a text file, not a database");

        Assert.Throws<DataFormatException>(() => SqliteDatabase.ListTables(_temp.File("fake.db")));
    }

    private object Scalar(string sql)
    {
        using var connection = new SqliteConnection($"Data Source={Db};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar()!;
    }
}
