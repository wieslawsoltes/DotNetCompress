# DotNetCompress

[![NuGet](https://img.shields.io/nuget/v/DotNetCompress.svg)](https://www.nuget.org/packages/DotNetCompress)
[![NuGet](https://img.shields.io/nuget/dt/DotNetCompress.svg)](https://www.nuget.org/packages/DotNetCompress)

[![GitHub release](https://img.shields.io/github/release/wieslawsoltes/DotNetCompress)](https://github.com/wieslawsoltes/DotNetCompress)
[![Github All Releases](https://img.shields.io/github/downloads/wieslawsoltes/DotNetCompress/total.svg)](https://github.com/wieslawsoltes/DotNetCompress)
[![Github Releases](https://img.shields.io/github/downloads/wieslawsoltes/DotNetCompress/latest/total.svg)](https://github.com/wieslawsoltes/DotNetCompress)

An .NET compression tool. Supported file formats are Brotli, GZip, ZLib and Deflate.

# Usage

### Install

```
dotnet tool install --global DotNetCompress --version 4.0.0
```

### Uninstall

```
dotnet tool uninstall -g DotNetCompress
```

### Command-line help

```
Description:
  An .NET compression tool.

Usage:
  DotNetCompress [options]

Options:
  -f, --inputFiles <inputFiles>      The relative or absolute path to 
                                     the input files
  -d, --inputDirectory               The relative or absolute path to 
  <inputDirectory>                   the input directory
  -o, --outputDirectory              The relative or absolute path to 
  <outputDirectory>                  the output directory
  --outputFiles <outputFiles>        The relative or absolute path to 
                                     the output files
  -p, --pattern <pattern>            The search string to match 
                                     against the names of files in the 
                                     input directory [default: *.*]
  --format <format>                  The compression file format (br, 
                                     gz, zlib, def) [default: br]
  -l, --level                        The compression level (Optimal, 
  <Fastest|NoCompression|Optimal|Sm  Fastest, NoCompression, 
  allestSize>                        SmallestSize) [default: 
                                     SmallestSize]
  -t, --threads <threads>            The number of parallel job 
                                     threads [default: 1]
  -r, --recursive                    Recurse into subdirectories of 
                                     input directory search
  --quiet                            Set verbosity level to quiet
  -?, -h, --help                     Show help and usage information
  --version                          Show version information
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

### ZLib example

```
dotnetcompress -d /publish/files/path -p "*.dll" --format zlib -l Optimal
dotnetcompress -d /publish/files/path -p "*.wasm" --format zlib -l Optimal
dotnetcompress -d /publish/files/path -p "*.js" --format zlib -l Optimal
dotnetcompress -d /publish/files/path -p "*.dll" -p "*.js" -p "*.wasm" --format zlib -l Optimal
```

### Deflate example

```
dotnetcompress -d /publish/files/path -p "*.dll" --format def -l Optimal
dotnetcompress -d /publish/files/path -p "*.wasm" --format def -l Optimal
dotnetcompress -d /publish/files/path -p "*.js" --format def -l Optimal
dotnetcompress -d /publish/files/path -p "*.dll" -p "*.js" -p "*.wasm" --format def -l Optimal
```

## License

DotNetCompress is licensed under the [MIT license](LICENSE.md).
