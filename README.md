# DotNetCompress

[![NuGet](https://img.shields.io/nuget/v/DotNetCompress.svg)](https://www.nuget.org/packages/DotNetCompress)
[![NuGet](https://img.shields.io/nuget/dt/DotNetCompress.svg)](https://www.nuget.org/packages/DotNetCompress)

[![GitHub release](https://img.shields.io/github/release/wieslawsoltes/DotNetCompress)](https://github.com/wieslawsoltes/DotNetCompress)
[![Github All Releases](https://img.shields.io/github/downloads/wieslawsoltes/DotNetCompress/total.svg)](https://github.com/wieslawsoltes/DotNetCompress)
[![Github Releases](https://img.shields.io/github/downloads/wieslawsoltes/DotNetCompress/latest/total.svg)](https://github.com/wieslawsoltes/DotNetCompress)

An .NET compression tool.

# Usage

```
dotnet tool install --global DotNetCompress --version 1.0.0-preview.3
```

```
DotNetCompress:
  An .NET compression tool.

Usage:
  DotNetCompress [options]

Options:
  -f, --inputFiles <inputfiles>                               The relative or absolute path to the input files
  -d, --inputDirectory <inputdirectory>                       The relative or absolute path to the input directory
  -o, --outputDirectory <outputdirectory>                     The relative or absolute path to the output directory
  --outputFiles <outputfiles>                                 The relative or absolute path to the output files
  -p, --pattern <pattern>                                     The search string to match against the names of files in the input directory
  --format <format>                                           The compression file format (br, gz)
  -l, --level <Fastest|NoCompression|Optimal|SmallestSize>    The compression level (Optimal, Fastest, NoCompression, SmallestSize)
  --version                                                   Show version information
  -?, -h, --help                                              Show help and usage information

```

```
dotnetcompress -d /publish/files/path -p *.dll --format br -l Optimal
dotnetcompress -d /publish/files/path -p *.dll --format gz -l Optimal
```

```
dotnetcompress -d /publish/files/path -p *.wasm --format br -l Optimal
dotnetcompress -d /publish/files/path -p *.wasm --format gz -l Optimal
```

## License

DotNetCompress is licensed under the [MIT license](LICENSE.md).
