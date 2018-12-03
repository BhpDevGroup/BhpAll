## Prerequisites

You will need Window or Linux. Use a virtual machine if you have a Mac. Ubuntu 14 and 16 are supported. Ubuntu 17 is not supported.

Install [.NET Core](https://www.microsoft.com/net/download/core).

On Linux, install the LevelDB and SQLite3 dev packages. E.g. on Ubuntu:

```sh
sudo apt-get install libleveldb-dev sqlite3 libsqlite3-dev libunwind8-dev

```

On Windows, use the [Bhp version of LevelDB](https://github.com/BhpAlpha/bhp-leveldb).

## Download release binaries

Download and unzip [latest release](https://github.com/BhpAlpha/bhp-cli/releases).

```sh
dotnet bhp-cli.dll
```

## Compile from source

Clone the bhp-cli [repository](https://github.com/BhpAlpha/bhp-cli.git)

```sh
cd bhp-cli
dotnet restore
dotnet publish -c Release
```
In order to run, you need .NET Core. Download the SDK [binary](https://www.microsoft.com/net/download/linux).

## Logging

To enable logs in bhp-cli, you need to add the ApplicationLogs plugin. Please check [here](https://github.com/BhpAlpha/bhp-plugins) for more information.