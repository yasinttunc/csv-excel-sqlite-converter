using ClosedXML.Excel;
using DataConverter.Core.Excel;

namespace DataConverter.Core.Tests;

public class ExcelFilesTests
{
    private static TabularData Sample() => TabularData.Create(
        ["Number", "Name", "Score", "Phone"],
        [["00123", "Şeyma Kaya", "87.5", "0555 111 22 33"], ["42", "İrem Aydın", "-3", ""]]);

    [Fact]
    public void Write_ThenRead_GivesTheSameTable()
    {
        using var stream = new MemoryStream();
        ExcelFiles.Write(Sample(), stream, "Grades");
        stream.Position = 0;

        var back = ExcelFiles.ReadSheet(stream);

        Assert.Equal(Sample().Headers, back.Headers);
        Assert.Equal(Sample().Rows, back.Rows);
    }

    [Fact]
    public void Write_StoresPlainNumbersAsNumbers_AndKeepsLeadingZerosAsText()
    {
        using var stream = new MemoryStream();
        ExcelFiles.Write(Sample(), stream);
        stream.Position = 0;

        using var book = new XLWorkbook(stream);
        var sheet = book.Worksheet(1);
        Assert.True(sheet.Cell(2, 3).Value.IsNumber);
        Assert.Equal(87.5, sheet.Cell(2, 3).Value.GetNumber());
        Assert.True(sheet.Cell(2, 1).Value.IsText);
        Assert.Equal("00123", sheet.Cell(2, 1).Value.GetText());
        Assert.True(sheet.Cell(3, 1).Value.IsNumber);
        Assert.True(sheet.Row(1).Style.Font.Bold);
    }

    [Theory]
    [InlineData("42", true)]
    [InlineData("-3.25", true)]
    [InlineData("0", true)]
    [InlineData("0.5", true)]
    [InlineData("1e3", true)]
    [InlineData("007", false)]
    [InlineData("1,5", false)]
    [InlineData(" 4", false)]
    [InlineData("1234567890123456", false)]
    [InlineData("abc", false)]
    public void TryNumber_OnlyAcceptsPlainNumbers(string text, bool expected)
    {
        Assert.Equal(expected, ExcelFiles.TryNumber(text, out _));
    }

    [Theory]
    [InlineData("Grades 2026", "Grades 2026")]
    [InlineData("a/b:c*d?", "a_b_c_d_")]
    [InlineData("", "Sheet1")]
    [InlineData("A very long sheet name that goes on and on", "A very long sheet name that goe")]
    public void SafeSheetName_FollowsExcelRules(string input, string expected)
    {
        Assert.Equal(expected, ExcelFiles.SafeSheetName(input));
    }

    [Fact]
    public void ReadSheet_ChoosesTheNamedSheet_AndListsSheets()
    {
        using var stream = Workbook(book =>
        {
            book.AddWorksheet("Notes").Cell(1, 1).Value = "Ignore";
            var data = book.AddWorksheet("Data");
            data.Cell(1, 1).Value = "Code";
            data.Cell(2, 1).Value = "TKM405";
        });

        Assert.Equal(new[] { "Notes", "Data" }, ExcelFiles.SheetNames(stream));
        var table = ExcelFiles.ReadSheet(new MemoryStream(stream.ToArray()), "data");
        Assert.Equal("TKM405", Assert.Single(table.Rows)[0]);
    }

    [Fact]
    public void ReadSheet_ConvertsDatesBooleansAndNumbersToText()
    {
        using var stream = Workbook(book =>
        {
            var sheet = book.AddWorksheet("S");
            sheet.Cell(1, 1).Value = "When";
            sheet.Cell(1, 2).Value = "Done";
            sheet.Cell(1, 3).Value = "Amount";
            sheet.Cell(2, 1).Value = new DateTime(2026, 11, 2);
            sheet.Cell(2, 2).Value = true;
            sheet.Cell(2, 3).Value = 1234.5;
        });

        var row = Assert.Single(ExcelFiles.ReadSheet(stream).Rows);

        Assert.Equal(new[] { "2026-11-02", "TRUE", "1234.5" }, row);
    }

    [Fact]
    public void ReadSheet_StartsAtTheFirstUsedCell_AndSkipsEmptyRows()
    {
        using var stream = Workbook(book =>
        {
            var sheet = book.AddWorksheet("S");
            sheet.Cell(3, 2).Value = "A";
            sheet.Cell(3, 3).Value = "B";
            sheet.Cell(4, 2).Value = "1";
            sheet.Cell(6, 3).Value = "2";
        });

        var table = ExcelFiles.ReadSheet(stream);

        Assert.Equal(new[] { "A", "B" }, table.Headers);
        Assert.Equal(2, table.RowCount);
        Assert.Equal(new[] { "", "2" }, table.Rows[1]);
    }

    [Fact]
    public void ReadSheet_RefusesMergedCells()
    {
        using var stream = Workbook(book =>
        {
            var sheet = book.AddWorksheet("S");
            sheet.Cell(1, 1).Value = "Title";
            sheet.Range("A1:C1").Merge();
        });

        var error = Assert.Throws<DataFormatException>(() => ExcelFiles.ReadSheet(stream));
        Assert.Contains("A1:C1", error.Message);
    }

    [Fact]
    public void ReadSheet_MissingSheetOrNotAWorkbook_IsReported()
    {
        using var stream = Workbook(book => book.AddWorksheet("Only"));
        Assert.Contains("Only", Assert.Throws<DataFormatException>(() => ExcelFiles.ReadSheet(stream, "Other")).Message);

        using var junk = new MemoryStream("not a workbook"u8.ToArray());
        Assert.Throws<DataFormatException>(() => ExcelFiles.ReadSheet(junk));
    }

    private static MemoryStream Workbook(Action<XLWorkbook> build)
    {
        using var book = new XLWorkbook();
        build(book);
        var stream = new MemoryStream();
        book.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
