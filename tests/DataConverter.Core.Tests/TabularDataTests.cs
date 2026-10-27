namespace DataConverter.Core.Tests;

public class TabularDataTests
{
    [Fact]
    public void Create_NamesBlankAndRepeatedHeaders()
    {
        var data = TabularData.Create(["Name", "", "name", " Name "], []);

        Assert.Equal(new[] { "Name", "Column 2", "name (2)", "Name (3)" }, data.Headers);
    }

    [Fact]
    public void Create_PadsShortRowsAndWidensForLongRows()
    {
        var data = TabularData.Create(["A", "B"], [["1"], ["1", "2", "3"]]);

        Assert.Equal(3, data.ColumnCount);
        Assert.Equal("Column 3", data.Headers[2]);
        Assert.Equal(new[] { "1", "", "" }, data.Rows[0]);
        Assert.Equal(new[] { "1", "2", "3" }, data.Rows[1]);
    }

    [Fact]
    public void IndexOf_IgnoresCase_AndTakeLimitsRows()
    {
        var data = TabularData.Create(["Section"], [["a"], ["b"], ["c"]]);

        Assert.Equal(0, data.IndexOf("SECTION"));
        Assert.Equal(-1, data.IndexOf("Missing"));
        Assert.Equal(2, data.Take(2).RowCount);
    }
}
