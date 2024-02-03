namespace ChDb;

[TestClass]
public class ChDbTest
{
    [TestMethod]
    public void QueryVersionTest()
    {
        var result = ChDb.Query("select version()");
        Assert.IsNotNull(result);
        Assert.AreEqual(1UL, result.RowsRead);
        Assert.AreEqual(52UL, result.BytesRead);
        Assert.AreEqual("23.10.1.1\n", result.Buf);
        Assert.IsNull(result.ErrorMessage);
        Assert.AreNotEqual(TimeSpan.Zero, result.Elapsed);
        Assert.IsTrue(0.1 > result.Elapsed.TotalSeconds);
    }

    [TestMethod]
    public void QueryErrorTest()
    {
        Assert.ThrowsException<ArgumentNullException>(() => ChDb.Query(null!));
        // TODO behavior changed in 1.2.1
        var r1 = ChDb.Query("wrong_query");
        Assert.IsNotNull(r1);
        Assert.IsNull(r1.Buf);
        Assert.IsNotNull(r1.ErrorMessage);

        var r2 = ChDb.Query("wrong_query", "PrettyCompact");
        Assert.IsNotNull(r2);
        Assert.IsNull(r2.Buf);
        Assert.IsNotNull(r2.ErrorMessage);

        var r3 = ChDb.Query("select version()", "wrong_format");
        Assert.IsNotNull(r3);
        Assert.IsNull(r3.Buf);
        StringAssert.Contains(r3.ErrorMessage, "Unknown output format");
    }

    [TestMethod]
    public void NoDataTest()
    {
        var result = ChDb.Query("create table x(a UInt8, b UInt8, c UInt8) Engine=Memory");
        Assert.IsNotNull(result);
        Assert.AreEqual(0UL, result.RowsRead);
        Assert.AreEqual(0UL, result.BytesRead);
        Assert.IsNull(result.Buf);
        Assert.IsNull(result.ErrorMessage);
        Assert.AreNotEqual(TimeSpan.Zero, result.Elapsed);
        Assert.IsTrue(0.1 > result.Elapsed.TotalSeconds);
    }

    [TestMethod]
    public void EmptyResultTest()
    {
        var result = ChDb.Query("show tables");
        Assert.IsNotNull(result);
        Assert.AreEqual(0UL, result.RowsRead);
        Assert.AreEqual(0UL, result.BytesRead);
        Assert.AreEqual("", result.Buf);
        Assert.IsNull(result.ErrorMessage);
        Assert.AreNotEqual(TimeSpan.Zero, result.Elapsed);
        Assert.IsTrue(0.1 > result.Elapsed.TotalSeconds);
    }

    [TestMethod]
    public void RowNumberTest()
    {
        var result = ChDb.Query("SELECT * FROM numbers(10)");
        Assert.IsNotNull(result);
        Assert.AreEqual(10UL, result.RowsRead);
    }

    [TestMethod]
    public void FormatTest()
    {
        Assert.AreEqual("1\t2\t3\n", ChDb.Query("SELECT 1 as a, 2 b, 3 c")!.Buf);
        Assert.AreEqual("1,2,3\n", ChDb.Query("SELECT 1 as a, 2 b, 3 c", "CSV")!.Buf);
        Assert.AreEqual("\"a\",\"b\",\"c\"\n1,2,3\n", ChDb.Query("SELECT 1 as a, 2 b, 3 c", "CSVWithNames")!.Buf);
        StringAssert.Contains(ChDb.Query("SELECT 1 as a, 2 b, 3 c", "CSVWithNamesAndTypes")!.Buf, "UInt8");
    }

    [TestMethod]
    public void InMemoryTest()
    {
        var sql =
            """
            create table test (a UInt8, b UInt8, c UInt8) Engine=Memory;
            insert into test values (1, 2, 3);
            select * from test; show tables;
            drop table test;show tables
            """;
        var result = ChDb.Query(sql);
        Assert.IsNotNull(result);
        Assert.AreEqual("", result.Buf);
        Assert.AreEqual(null, result.ErrorMessage);
    }

    [TestMethod]
    public void S3ParquetTest()
    {
        var result = ChDb.Query("DESCRIBE s3('https://datasets-documentation.s3.eu-west-3.amazonaws.com/house_parquet/house_0.parquet')");
        Assert.IsNotNull(result);
        Assert.IsNull(result.ErrorMessage);
        StringAssert.StartsWith(result.Buf, "price\tNullable(Int64)");
    }

    [TestMethod]
    public void S3CountTest()
    {
        var result = ChDb.Query(
            """
            SELECT count()
            FROM s3('https://datasets-documentation.s3.eu-west-3.amazonaws.com/house_parquet/house_0.parquet')
            """);
        Assert.IsNotNull(result);
        Assert.IsNull(result.ErrorMessage);
        Assert.IsTrue(int.TryParse(result.Buf, out var count));
        Assert.AreEqual(2772030, count);
    }

    [TestMethod]
    public void CsvTest() {
        var csv = """
            Name, Age, City
            John, 25, New York
            Alice, 30, London
            Bob, 22, Tokyo
            Eva, 28, Paris
            """;
        var dataPath = "/tmp/chdb/data";
        Directory.CreateDirectory(dataPath);
        File.WriteAllText(Path.Combine(".", "test.csv"), csv);
        var session = new Session
        {
            Format = "PrettyCompact",
            DataPath = dataPath,
            LogLevel = "trace",
        };
        var result = session.Execute("SELECT * FROM 'test.csv'", "CSVWithNamesAndTypes");
        Assert.IsNotNull(result);
        Assert.AreEqual(4UL, result.RowsRead);
        Assert.AreEqual(155UL, result.BytesRead);
        StringAssert.StartsWith(result.Buf,
            """
            "Name","Age","City"
            """);
    }

    [TestMethod]
    public void SessionTest()
    {
        using var session = new Session
        {
            Format = "PrettyCompact",
            LogLevel = "trace",
        };
        var nr = "xyz";

        Assert.IsTrue(session.Execute($"SHOW DATABASES")?.Buf?.Contains("_local")); // there is _local database instead of default
        Assert.AreEqual("", session.Execute($"SHOW TABLES")?.Buf);
        StringAssert.Contains(session.Execute($"SELECT currentDatabase()")?.Buf, "_local");

        var r1 = session.Execute($"DROP DATABASE IF EXISTS db_{nr}");
        Assert.IsNotNull(r1);
        Assert.IsNull(r1.Buf);
        Assert.IsNull(r1.ErrorMessage);

        var r2 = session.Execute($"CREATE DATABASE IF NOT EXISTS db_{nr} ENGINE = Atomic");
        Assert.IsNotNull(r2);
        Assert.IsNull(r2.Buf);
        Assert.IsNull(r2.ErrorMessage);

        var r3 = session.Execute($"CREATE TABLE IF NOT EXISTS db_{nr}.log_table_{nr} (x String, y Int) ENGINE = Log;");
        Assert.IsNotNull(r3);
        Assert.IsNull(r3.Buf);
        Assert.IsNull(r3.ErrorMessage);

        var r4 = session.Execute($"INSERT INTO db_{nr}.log_table_{nr} VALUES ('a', 1), ('b', 3), ('c', 2), ('d', 5);");
        Assert.IsNotNull(r4);
        Assert.IsNull(r4.Buf);
        Assert.IsNull(r4.ErrorMessage);

        var r5 = session.Execute($"SELECT * FROM db_{nr}.log_table_{nr}", "TabSeparatedWithNames");
        Assert.IsNotNull(r5);
        Assert.AreEqual("x\ty\na\t1\nb\t3\nc\t2\nd\t5\n", r5.Buf);
        Assert.IsNull(r5.ErrorMessage);

        var r6 = session.Execute($"CREATE VIEW db_{nr}.view_{nr} AS SELECT * FROM db_{nr}.log_table_{nr} LIMIT 4;");
        Assert.IsNotNull(r6);
        Assert.IsNull(r6.Buf);
        Assert.IsNull(r6.ErrorMessage);

        var r7 = session.Execute($"SELECT * FROM db_{nr}.view_{nr}", "TabSeparatedWithNames");
        Assert.IsNotNull(r7);
        Assert.AreEqual("x\ty\na\t1\nb\t3\nc\t2\nd\t5\n", r7.Buf);
        Assert.IsNull(r7.ErrorMessage);

        session.Execute($"DROP DATABASE IF EXISTS db_{nr}");
    }
}