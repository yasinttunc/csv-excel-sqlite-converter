using DataConverter.Cli;
using DataConverter.Core.Sqlite;

namespace DataConverter.Core.Tests;

public class CommandLineTests : IDisposable
{
    private readonly TempFolder _temp = new();

    public void Dispose() => _temp.Dispose();

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var code = CommandLine.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public void NoArguments_PrintsUsage_WithUsageExitCode()
    {
        var (code, output, _) = Run();

        Assert.Equal(CommandLine.UsageError, code);
        Assert.Contains("dataconv convert", output);
    }

    [Fact]
    public void Help_And_Version_Succeed()
    {
        Assert.Equal(CommandLine.Ok, Run("help").Code);
        Assert.StartsWith("dataconv ", Run("--version").Out);
    }

    [Fact]
    public void Convert_CsvToExcel_AndBack()
    {
        var xlsx = _temp.File("grades.xlsx");
        var csv = _temp.File("grades.csv");

        var toExcel = Run("convert", TempFolder.Sample("grades-tr.csv"), xlsx);
        var toCsv = Run("convert", xlsx, csv, "--out-delimiter", ";");

        Assert.Equal(CommandLine.Ok, toExcel.Code);
        Assert.Contains("48 rows", toExcel.Out);
        Assert.Equal(CommandLine.Ok, toCsv.Code);
        var original = DataFiles.Load(TempFolder.Sample("grades-tr.csv"));
        var roundTrip = DataFiles.Load(csv);
        Assert.Equal(original.Headers, roundTrip.Headers);
        Assert.Equal(original.Rows, roundTrip.Rows);
    }

    [Fact]
    public void Import_SplitBySection_ThenListAndExport()
    {
        var db = _temp.File("school.db");

        var import = Run("import", TempFolder.Sample("grades.csv"), db, "--split-by", "Section");
        var tables = Run("tables", db);
        var export = Run("export", db, "TKM405-41", _temp.File("tkm405-41.xlsx"));

        Assert.Equal(CommandLine.Ok, import.Code);
        Assert.Contains("Imported 48 rows into 4 table(s)", import.Out);
        Assert.Contains("Midterm INTEGER", import.Out);
        Assert.Contains("TKM439-41", tables.Out);
        Assert.Equal(CommandLine.Ok, export.Code);
        Assert.Equal(12, DataFiles.Load(_temp.File("tkm405-41.xlsx")).RowCount);
    }

    [Fact]
    public void Import_TwiceWithoutIfExists_FailsWithDataErrorCode()
    {
        var db = _temp.File("school.db");
        Run("import", TempFolder.Sample("grades.csv"), db);

        var (code, _, error) = Run("import", TempFolder.Sample("grades.csv"), db);

        Assert.Equal(CommandLine.DataError, code);
        Assert.Contains("Replace or Append", error);
        Assert.Equal(CommandLine.Ok, Run("import", TempFolder.Sample("grades.csv"), db, "--if-exists", "append").Code);
        Assert.Equal(96L, SqliteDatabase.ListTables(db)[0].Rows);
    }

    [Theory]
    [InlineData("frobnicate")]
    [InlineData("convert", "only-one-argument.csv")]
    [InlineData("import", "a.csv", "b.db", "--if-exists", "sometimes")]
    [InlineData("convert", "a.csv", "b.xlsx", "--colour", "red")]
    [InlineData("convert", "a.csv", "b.xlsx", "--delimiter", "#")]
    [InlineData("preview", "a.csv", "--rows")]
    public void WrongUsage_ReturnsUsageErrorCode(params string[] args)
    {
        var (code, _, error) = Run(args);

        Assert.Equal(CommandLine.UsageError, code);
        Assert.StartsWith("error:", error);
    }

    [Fact]
    public void MissingFile_ReturnsDataErrorCode()
    {
        var (code, _, error) = Run("preview", _temp.File("missing.csv"));

        Assert.Equal(CommandLine.DataError, code);
        Assert.Contains("does not exist", error);
    }

    [Fact]
    public void Preview_PrintsAlignedRows()
    {
        var (code, output, _) = Run("preview", TempFolder.Sample("grades.csv"), "--rows", "3");

        Assert.Equal(CommandLine.Ok, code);
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("Student number | Name", lines[0]);
        Assert.Contains("48 rows, 8 columns.", output);
        Assert.Equal(6, lines.Length);
    }

    [Fact]
    public void UnsupportedFileType_IsExplained()
    {
        File.WriteAllText(_temp.File("old.xls"), "x");

        var (code, _, error) = Run("preview", _temp.File("old.xls"));

        Assert.Equal(CommandLine.DataError, code);
        Assert.Contains("save it as .xlsx", error);
    }
}
