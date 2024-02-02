namespace test;

using ChDb;

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
    [Ignore("Bugfix is in v1.2.1")]
    public void QueryNullTest()
    {
        Assert.ThrowsException<ArgumentNullException>(() => ChDb.Query(null!));
        //Assert.ThrowsException<ArgumentException>(() => ChDb.Query("wrong_query"));
        //Assert.ThrowsException<ArgumentException>(() => ChDb.Query("wrong_query", "PrettyCompact"));
        //Assert.ThrowsException<ArgumentException>(() => ChDb.Query("select version()", "wrong_format"));
        // TODO behavior changed in 1.2.1
        Assert.IsNull(ChDb.Query("wrong_query"));
        Assert.IsNull(ChDb.Query("wrong_query", "PrettyCompact"));
        Assert.IsNull(ChDb.Query("select version()", "wrong_format"));
    }

    [TestMethod]
    [Ignore("Bugfix is in v1.2.1")]
    public void NoDataTest()
    {
        var result = ChDb.Query("create table x(a UInt8, b UInt8, c UInt8) Engine=Memory");
        Assert.IsNotNull(result);
        Assert.AreEqual(0UL, result.RowsRead);
        Assert.AreEqual(0UL, result.BytesRead);
        Assert.AreEqual("", result.Buf);
        Assert.IsNotNull(result.ErrorMessage);
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
        //clickhouse local -q "DESCRIBE s3('https://datasets-documentation.s3.eu-west-3.amazonaws.com/house_parquet/house_0.parquet')"
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
    [Ignore("Bugfix is in v1.2.1")]
    public void SessionTest()
    {
        var session = new Session
        {
            Format = "PrettyCompact",
            DataPath = "/tmp/chdb/data",
            UdfPath = "/tmp/chdb/udf",
            LogLevel = "trace",
        };
        // var r1 = session.Execute("select 1");
        // Assert.IsNotNull(r1);
        // Assert.AreEqual("1\n", r1.Buf);

        var nr = "xyz";
        var r2 = session.Execute($"CREATE DATABASE IF NOT EXISTS db_{nr} ENGINE = Atomic");
        //Assert.IsNotNull(r2);
        var r3 = session.Execute($"CREATE TABLE IF NOT EXISTS db_{nr}.log_table_xxx (x String, y Int) ENGINE = Log;");
        // Assert.IsNotNull(r3);
        var r4 = session.Execute($"INSERT INTO db_{nr}.log_table_{nr} VALUES ('a', 1), ('b', 3), ('c', 2), ('d', 5);");
        // Assert.IsNotNull(r4);
        var r5 = session.Execute($"SELECT * FROM db_{nr}.log_table_{nr}", "TabSeparatedWithNames");
        Assert.IsNotNull(r5);
        Assert.AreEqual("x\ty\na\t1\nb\t3\nc\t2\nd\t5\n", r5.Buf);
        var r6 = session.Execute($"CREATE VIEW db_{nr}.view_{nr} AS SELECT * FROM db_{nr}.log_table_{nr} LIMIT 4;");
        // Assert.IsNotNull(r6);
        var r7 = session.Execute($"SELECT * FROM db_{nr}.view_{nr}", "TabSeparatedWithNames");
        Assert.IsNotNull(r7);
        Assert.AreEqual("x\ty\na\t1\nb\t3\nc\t2\nd\t5\n", r7.Buf);
    }
}