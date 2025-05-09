# chdb

A .NET Core binding for [chdb](https://doc.chdb.io) library.

![GitHub License](https://img.shields.io/github/license/chdb-io/chdb-dotnet)
![example workflow](https://github.com/chdb-io/chdb-dotnet/actions/workflows/dotnet.yml/badge.svg)
![NuGet Version](https://img.shields.io/nuget/vpre/chdb)
![NuGet Downloads](https://img.shields.io/nuget/dt/chdb)

### Architecture

<div align="center">
  <img src="https://github.com/chdb-io/chdb-dotnet/raw/main/chdb-dotnet.png" width="450">
</div>

### Usage

Running on platforms: linux, osx, windows, and architectures: x64, arm64.

>Note for windows users - there is no windows bild in sight, but you can still use it in WSL.

Currently the librairy is too large to be packed into a nuget package, so you need to install it manually. Use the [update_libchdb.sh](update_libchdb.sh) script to download the library for your platform and architecture.

```bash
# download the latest version of the library - it takes a version as an optional argument
./update_libchdb.sh
# install the package to your project
dotnet add package chdb
```

Also place the library in appropriate folder, and add following to your csproj file:

```xml
  <ItemGroup>
    <None Update="libchdb.so">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

Then you can use it in your code like this:

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

or use it right in F# interactive with `dotnet fsi`:

```fsharp
#r "nuget: chdb"

open ChDb

// print out result in the PrettyCompact format by default
let result = ChDb.Query "select version()"
printfn "%s" result.Text
// or save result to a text or binary file in any supported format
let result = ChDb.Query("select * from system.formats where is_output = 1", "CSVWithNames")
System.IO.File.WriteAllBytes("supported_formats.csv", result.Buf)
```

## chdb-tool

![NuGet Version](https://img.shields.io/nuget/vpre/chdb-tool)
![NuGet Downloads](https://img.shields.io/nuget/dt/chdb-tool)

This is a dotnet tool, running [chdb](https://doc.chdb.io) library.
Probably you better served using the clickhouse client and run `clickhouse local`, but maybe it is more useful in some cases.

### Installation

Requires .NET SDK 6.0 or later.

```bash
dotnet tool install --global chdb-tool
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
./update_libchdb.sh [v2.0.4]
cp libchdb.so src/chdb/
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release
dotnet nuget add source ./nupkg --name chdb
dotnet tool update -g chdb-tool
chdb --version
```

## Authors

* [Andreas Vilinski](https://github.com/vilinski)
* [Auxten](https://github.com/auxten)
