# chdb

A .NET Core binding for [chdb](https://doc.chdb.io) library.

![GitHub License](https://img.shields.io/github/license/chdb-io/chdb-dotnet)
![example workflow](https://github.com/chdb-io/chdb-dotnet/actions/workflows/dotnet.yml/badge.svg)

<table>
    <thead>
        <tr>
            <th>OS</th>
            <th>Arch</th>
            <th>chdb</th>
            <th>downloads</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td rowspan="2">OSX</td>
            <td>arm64</td>
            <td><img src="https://img.shields.io/nuget/vpre/chdb-osx-arm64"></td>
            <td><img src="https://img.shields.io/nuget/dt/chdb-osx-arm64"></td>
        </tr>
        <tr>
            <td>x64</td>
            <td><img src="https://img.shields.io/nuget/vpre/chdb-osx-x64"></td>
            <td><img src="https://img.shields.io/nuget/dt/chdb-osx-x64"></td>
        </tr>
        <tr>
            <td rowspan="2">Linux</td>
            <td>arm64</td>
            <td><img src="https://img.shields.io/nuget/vpre/chdb-linux-arm64"></td>
            <td><img src="https://img.shields.io/nuget/dt/chdb-linux-arm64"></td>
        </tr>
        <tr>
            <td>x64</td>
            <td><img src="https://img.shields.io/nuget/vpre/chdb-linux-x64"></td>
            <td><img src="https://img.shields.io/nuget/dt/chdb-linux-x64"></td>
        </tr>
    </tbody>
</table>

### Usage

```bash
dotnet add package chdb
```

```csharp
using ChDb;

var result = ChDb.Query("select version()");
Console.WriteLine(result.Text);
// 23.10.1.1
var s = new Session();
var result = s.Query("select * from system.formats where is_output = 1", "PrettyCompact");
// ┌─name───────────────────────────────────────┬─is_input─┬─is_output─┬─supports_parallel_parsing─┬─supports_parallel_formatting─┐
// │ Prometheus                                 │        0 │         1 │                         0 │                            0 │
// │ PostgreSQLWire                             │        0 │         1 │                         0 │                            0 │
// │ MySQLWire                                  │        0 │         1 │                         0 │                            0 │
// │ JSONEachRowWithProgress                    │        0 │         1 │                         0 │                            0 │
// │ ODBCDriver2                                │        0 │         1 │                         0 │                            0 │
// ...
var result = s.Query("DESCRIBE s3('https://datasets-documentation.s3.eu-west-3.amazonaws.com/house_parquet/house_0.parquet')");
Console.WriteLine(result.Text);
```

## chdb-tool

![NuGet Version](https://img.shields.io/nuget/vpre/chdb-tool)
![NuGet Downloads](https://img.shields.io/nuget/dt/chdb-tool)

This is a dotnet tool, running [chdb](https://doc.chdb.io) library.
Actually you better install the clickhouse client and run `clickhouse local`, but maybe it is more useful for some cases.

>Note for windows users - there is no windows bild in sight, but you can use it in WSL.

### Installation

Requires .NET SDK 6.0 or later.

```bash
#dotnet tool install --global chdb-tool-<OS>-<ARCH>
dotnet tool install --global chdb-tool-linux-x64
```

OS supported: linux, osx
ARCH supported: x64, arm64

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
./update_libchdb.sh [v1.2.1]
# install versionbump tool
dotnet tool install -g BumpVersion
# bump version
bumpversion patch
git push --foloow-tags
```

## Authors

* [Andreas Vilinski](https://github.com/vilinski)
