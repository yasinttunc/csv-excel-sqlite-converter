using System.Text;
using DataConverter.Core.Csv;

namespace DataConverter.Core.Tests;

public class CsvFormatTests
{
    [Fact]
    public void Parse_HandlesQuotesDelimitersAndLineBreaksInsideFields()
    {
        var data = CsvFormat.Parse("Name,Note\r\n\"Morgan, Alex\",\"Said \"\"hi\"\"\nthen left\"\r\nSam,plain\r\n", ',');

        Assert.Equal(new[] { "Name", "Note" }, data.Headers);
        Assert.Equal(new[] { "Morgan, Alex", "Said \"hi\"\nthen left" }, data.Rows[0]);
        Assert.Equal(new[] { "Sam", "plain" }, data.Rows[1]);
    }

    [Theory]
    [InlineData("a,b,c\n1,2,3\n", ',')]
    [InlineData("a;b;c\n1;2,5;3\n", ';')]
    [InlineData("a\tb\n1\t2\n", '\t')]
    [InlineData("a|b\n1|2\n", '|')]
    [InlineData("\"x;y\",b\n\"1;2\",3\n", ',')]
    [InlineData("single column\nvalue\n", ',')]
    public void DetectDelimiter_FindsTheSeparator(string text, char expected)
    {
        Assert.Equal(expected, CsvFormat.DetectDelimiter(text));
    }

    [Fact]
    public void Parse_SkipsBlankLinesAndByteOrderMark()
    {
        var data = CsvFormat.Parse("﻿a,b\n\n1,2\n , \n3,4");

        Assert.Equal(new[] { "a", "b" }, data.Headers);
        Assert.Equal(2, data.RowCount);
        Assert.Equal(new[] { "3", "4" }, data.Rows[1]);
    }

    [Fact]
    public void Parse_WithoutHeader_NamesColumns()
    {
        var data = CsvFormat.Parse("1,2\n3,4", ',', hasHeader: false);

        Assert.Equal(new[] { "Column 1", "Column 2" }, data.Headers);
        Assert.Equal(2, data.RowCount);
    }

    [Fact]
    public void Parse_EmptyText_GivesAnEmptyTable()
    {
        Assert.Equal(0, CsvFormat.Parse(string.Empty).ColumnCount);
    }

    [Fact]
    public void Write_QuotesOnlyWhatNeedsIt_AndRoundTrips()
    {
        var data = TabularData.Create(["Name", "Note"], [["Morgan, Alex", "Said \"hi\""], ["Sam", " padded "], ["Lee", "line\nbreak"]]);

        var csv = CsvFormat.Write(data);

        Assert.StartsWith("Name,Note\r\n\"Morgan, Alex\",\"Said \"\"hi\"\"\"\r\nSam,\" padded \"\r\n", csv);
        var back = CsvFormat.Parse(csv, ',');
        Assert.Equal(data.Rows, back.Rows);
    }

    [Fact]
    public void Write_UsesTheChosenDelimiter()
    {
        var csv = CsvFormat.Write(TabularData.Create(["a", "b"], [["1,5", "2"]]), ';');

        Assert.Equal("a;b\r\n1,5;2\r\n", csv);
    }

    [Fact]
    public void Decode_ReadsUtf8WithAndWithoutBom()
    {
        var withBom = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("Şube")).ToArray();

        Assert.Equal("Şube", TextFiles.Decode(withBom, out var first));
        Assert.Equal("Şube", TextFiles.Decode(Encoding.UTF8.GetBytes("Şube"), out var second));
        Assert.Equal("utf-8", first.WebName);
        Assert.Equal("utf-8", second.WebName);
    }

    [Fact]
    public void Decode_FallsBackToTurkishWindowsEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = Encoding.GetEncoding(1254).GetBytes("Öğrenci No;Şube");

        Assert.Equal("Öğrenci No;Şube", TextFiles.Decode(bytes, out var encoding));
        Assert.Equal(1254, encoding.CodePage);
    }

    [Fact]
    public void Decode_ReadsUtf16WithBom()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("İrem")).ToArray();

        Assert.Equal("İrem", TextFiles.Decode(bytes, out _));
    }

    [Fact]
    public void SampleFile_TurkishExcelExport_IsReadCorrectly()
    {
        var data = DataFiles.Load(TempFolder.Sample("grades-tr.csv"));

        Assert.Equal(new[] { "Öğrenci No", "Ad Soyad", "Şube", "Vize", "Final", "Ortalama", "Telefon", "Not" }, data.Headers);
        Assert.Equal(48, data.RowCount);
        Assert.Contains(data.Rows, r => r[7] == "Late submission; accepted");
    }
}
