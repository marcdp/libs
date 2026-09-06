using System.Data;
using DProjects.Db.Readers;

namespace DProjects.Db.Tests;

public class DBReaderContractTests {

    [Fact]
    public async Task DBReaderDBTable_ImplementsSharedCursorContract() {
        var table = CreateTable(
            ["id", "name"],
            [
                [1, "one"],
                [2, "two"],
                [3, "three"],
                [4, "four"]
            ]);

        using var reader = new DBReaderDBTable(table);

        await AssertMixedReadContract(reader, table.Rows.Select(row => row.Values).ToArray());
    }

    [Fact]
    public async Task DBReaderRaw_ImplementsSharedCursorContract() {
        using var source = new StringReader("a b\nc d\ne f\ng h");
        using var reader = new DBReaderRaw(source, true);

        await AssertMixedReadContract(reader, [
            ["a", "b"],
            ["c", "d"],
            ["e", "f"],
            ["g", "h"]
        ]);
    }

    [Fact]
    public async Task DBReaderXml_ImplementsSharedCursorContract() {
        const string xml = """
            <table>
              <columns>
                <column name="id" dbtype="System.Int32" />
                <column name="name" dbtype="System.String" />
              </columns>
              <rows>
                <row><id>1</id><name>one</name></row>
                <row><id>2</id><name>two</name></row>
                <row><id>3</id><name>three</name></row>
                <row><id>4</id><name>four</name></row>
              </rows>
            </table>
            """;
        using var reader = new DBReaderXml(new StringReader(xml), false);

        Assert.Equal("id", reader.GetColumns()[0].Name);
        Assert.Equal(typeof(int), reader.GetColumns()[0].DBType);
        await AssertMixedReadContract(reader, [
            [1, "one"],
            [2, "two"],
            [3, "three"],
            [4, "four"]
        ]);
    }

    [Fact]
    public async Task DBReaderView_ArrayReadsPreserveProjectionRenamingOffsetAndLimit() {
        var table = CreateTable(
            ["id", "name", "ignored"],
            [
                [1, "one", "a"],
                [2, "two", "b"],
                [3, "three", "c"],
                [4, "four", "d"],
                [5, "five", "e"]
            ]);
        using var source = new DBReaderDBTable(table);
        using var reader = new DBReaderView(
            source,
            ["label=name", "id"],
            "",
            [],
            "",
            true,
            offset: 1,
            limit: 4);

        Assert.Equal(new[] { "label", "id" }, reader.GetColumns().Select(column => column.Name));
        await AssertMixedReadContract(reader, [
            ["two", 2],
            ["three", 3],
            ["four", 4],
            ["five", 5]
        ]);
        Assert.Equal(5, reader.Count);
    }

    [Fact]
    public async Task DBReaderCsv_ImplementsSharedCursorContract() {
        using var source = new StringReader("id,name\n1,one\n2,two\n3,three\n4,four");
        using var reader = new DBReaderCsv(source, true);

        Assert.Equal(new[] { "id", "name" }, reader.GetColumns().Select(column => column.Name));
        await AssertMixedReadContract(reader, [
            ["1", "one"],
            ["2", "two"],
            ["3", "three"],
            ["4", "four"]
        ]);
    }

    [Fact]
    public async Task DBReaderPlain_ImplementsSharedCursorContract() {
        using var source = new StringReader(
            "id  name\n" +
            "--  ----\n" +
            "1   one \n" +
            "2   two \n" +
            "3   three\n" +
            "4   four");
        using var reader = new DBReaderPlain(source, true);

        await AssertMixedReadContract(reader, [
            ["1", "one"],
            ["2", "two"],
            ["3", "thre"],
            ["4", "four"]
        ]);
    }

    [Fact]
    public async Task DBReaderXmlDocuments_ImplementsSharedCursorContract() {
        const string xml = """
            <document><id>1</id><name>one</name></document>
            <document><id>2</id><name>two</name></document>
            <document><id>3</id><name>three</name></document>
            <document><id>4</id><name>four</name></document>
            """;
        using var reader = new DBReaderXmlDocuments(new StringReader(xml), false);

        await AssertMixedReadContract(reader, [
            ["1", "one"],
            ["2", "two"],
            ["3", "three"],
            ["4", "four"]
        ]);
    }

    [Fact]
    public async Task DBReaderDbDataReader_ImplementsSharedCursorContract() {
        var dataTable = new DataTable();
        dataTable.Columns.Add("id", typeof(int));
        dataTable.Columns.Add("name", typeof(string));
        dataTable.Rows.Add(1, "one");
        dataTable.Rows.Add(2, "two");
        dataTable.Rows.Add(3, "three");
        dataTable.Rows.Add(4, "four");
        using var reader = new DBReaderDbDataReader(dataTable.CreateDataReader());

        await AssertMixedReadContract(reader, [
            [1, "one"],
            [2, "two"],
            [3, "three"],
            [4, "four"]
        ]);
    }

    private static DBTable CreateTable(string[] columns, object?[][] rows) {
        var table = new DBTable();
        foreach (var column in columns) table.Columns.Add(column);
        foreach (var row in rows) table.Rows.Add(row);
        return table;
    }

    private static async Task AssertMixedReadContract(IDBReader reader, object?[][] expectedRows) {
        Assert.Equal(reader.GetColumnsCount(), reader.GetColumns().Count);
        Assert.Equal(reader.GetColumnsCount(), (await reader.GetColumnsAsync()).Count);
        Assert.Equal(4, expectedRows.Length);

        Assert.Equal(expectedRows[0], Assert.IsType<DBRow>(reader.Read()).Values);

        var values = new object?[reader.GetColumnsCount()];
        Assert.True(reader.Read(values));
        Assert.Equal(expectedRows[1], values);

        Assert.Equal(expectedRows[2], Assert.IsType<DBRow>(await reader.ReadAsync()).Values);

        values = new object?[reader.GetColumnsCount()];
        Assert.True(await reader.ReadAsync(values));
        Assert.Equal(expectedRows[3], values);

        Assert.Null(reader.Read());
        Assert.False(reader.Read(new object?[reader.GetColumnsCount()]));
        Assert.Null(await reader.ReadAsync());
        Assert.False(await reader.ReadAsync(new object?[reader.GetColumnsCount()]));
        Assert.False(reader.NextResult());
        Assert.False(await reader.NextResultAsync());
    }
}
