# DotNetCompress

[![NuGet](https://img.shields.io/nuget/v/DotNetCompress.svg)](https://www.nuget.org/packages/DotNetCompress)
[![NuGet](https://img.shields.io/nuget/dt/DotNetCompress.svg)](https://www.nuget.org/packages/DotNetCompress)

[![GitHub release](https://img.shields.io/github/release/wieslawsoltes/DotNetCompress)](https://github.com/wieslawsoltes/DotNetCompress)
[![Github All Releases](https://img.shields.io/github/downloads/wieslawsoltes/DotNetCompress/total.svg)](https://github.com/wieslawsoltes/DotNetCompress)
[![Github Releases](https://img.shields.io/github/downloads/wieslawsoltes/DotNetCompress/latest/total.svg)](https://github.com/wieslawsoltes/DotNetCompress)

An .NET compression tool. Supported file formats are Brotli and GZip.

# Usage

### Install

```
dotnet tool install --global DotNetCompress --version 3.0.0
```

### Uninstall

```
dotnet tool uninstall -g DotNetCompress
```

### Command-line help

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
  -t, --threads <threads>                                     The number of parallel job threads
  -r, --recursive                                             Recurse into subdirectories of input directory search
  --quiet                                                     Set verbosity level to quiet
  --version                                                   Show version information
  -?, -h, --help                                              Show help and usage information
```

### Brotli example

```
dotnetcompress -d /publish/files/path -p "*.dll" --format br -l Optimal
dotnetcompress -d /publish/files/path -p "*.wasm" --format br -l Optimal
dotnetcompress -d /publish/files/path -p "*.js" --format br -l Optimal
dotnetcompress -d /publish/files/path -p "*.dll" -p "*.js" -p "*.wasm" --format br -l Optimal
```

### GZip example

```
dotnetcompress -d /publish/files/path -p "*.dll" --format gz -l Optimal
dotnetcompress -d /publish/files/path -p "*.wasm" --format gz -l Optimal
dotnetcompress -d /publish/files/path -p "*.js" --format gz -l Optimal
dotnetcompress -d /publish/files/path -p "*.dll" -p "*.js" -p "*.wasm" --format gz -l Optimal
```

## License

DotNetCompress is licensed under the [MIT license](LICENSE.md).
