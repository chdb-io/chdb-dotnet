# chdb

[![Build Status](https://dev.azure.com/ChDB/ChDB/_apis/build/status/ChDB.chdb?branchName=master)](https://dev.azure.com/ChDB/ChDB/_build/latest?definitionId=1&branchName=master)
[![NuGet](https://img.shields.io/nuget/v/ChDB.svg)](https://www.nuget.org/packages/ChDB/)
[![NuGet](https://img.shields.io/nuget/dt/ChDB.svg)](https://www.nuget.org/packages/ChDB/)
[![License](https://img.shields.io/github/license/ChDB/chdb.svg)](https://github.com/vilinski/chdb/LICENSE.md)

## chdb NuGet package

A .NET Core binding for [chdb](https://doc.chdb.io) library.


### Installation

```bash
dotnet add package chdb
```

### Usage

```csharp
using ChDb;

var result = ChDb.Query("select version()");
Console.WriteLine(result);
// 23.10.1.1
var result = ChDb.Query("select * from system.formats where is_output = 1", "PrettyCompact");
// ┌─name───────────────────────────────────────┬─is_input─┬─is_output─┬─supports_parallel_parsing─┬─supports_parallel_formatting─┐
// │ Prometheus                                 │        0 │         1 │                         0 │                            0 │
// │ PostgreSQLWire                             │        0 │         1 │                         0 │                            0 │
// │ MySQLWire                                  │        0 │         1 │                         0 │                            0 │
// │ JSONEachRowWithProgress                    │        0 │         1 │                         0 │                            0 │
// │ ODBCDriver2                                │        0 │         1 │                         0 │                            0 │
// ...
var result = ChDb.Query("DESCRIBE s3('https://datasets-documentation.s3.eu-west-3.amazonaws.com/house_parquet/house_0.parquet')");
Console.WriteLine(result.Buf); 
```

## chdb-tool

This is a dotnet tool for [chdb](https://doc.chdb.io) library. 
Actually you better just install clickhouse client and run `clickhouse local`

### Installation

```bash
dotnet tool install -g chdb-tool
```

### Usage

Try any of this commands lines to see which output you get.

```bash
chdb
chdb --version
chdb --help
chdb "select version()" 
chdb "select * from system.formats where is_output = 1" PrettyCompact
```

# Build

```bash
# update latest chdb version
./update_libchdb.sh
# install versionbump tool
dotnet tool install -g BumpVersion
# bump version
bumpversion patch
git push --foloow-tags
```

## Authors

* [Andreas Vilinski](https://github.com/vilinski)