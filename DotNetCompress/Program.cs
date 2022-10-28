using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Compression;
using DotNetCompress.Core;

var optionInputFiles = new Option(
    new[] {"--inputFiles", "-f"},
    "The relative or absolute path to the input files")
{
    Argument = new Argument<FileInfo[]?>(getDefaultValue: () => null)
};

var optionInputDirectory = new Option(
    new[] {"--inputDirectory", "-d"},
    "The relative or absolute path to the input directory")
{
    Argument = new Argument<DirectoryInfo?>(getDefaultValue: () => null)
};

var optionOutputDirectory = new Option(
    new[] {"--outputDirectory", "-o"},
    "The relative or absolute path to the output directory")
{
    Argument = new Argument<DirectoryInfo?>(getDefaultValue: () => null)
};

var optionOutputFiles = new Option(
    new[] {"--outputFiles"},
    "The relative or absolute path to the output files")
{
    Argument = new Argument<FileInfo[]?>(getDefaultValue: () => null)
};

var optionPattern = new Option(
    new[] {"--pattern", "-p"},
    "The search string to match against the names of files in the input directory")
{
    Argument = new Argument<string[]?>(getDefaultValue: () => new[] {"*.*"})
};

var optionFormat = new Option(
    new[] {"--format"},
    "The compression file format (br, gz)")
{
    Argument = new Argument<string>(getDefaultValue: () => "br")
};

var optionLevel = new Option(
    new[] {"--level", "-l"},
    "The compression level (Optimal, Fastest, NoCompression, SmallestSize)")
{
    Argument = new Argument<CompressionLevel>(getDefaultValue: () => CompressionLevel.SmallestSize)
};

var optionThreads = new Option(
    new[] {"--threads", "-t"},
    "The number of parallel job threads")
{
    Argument = new Argument<int>(getDefaultValue: () => 1)
};

var optionRecursive = new Option(
    new[] {"--recursive", "-r"},
    "Recurse into subdirectories of input directory search")
{
    Argument = new Argument<bool>(getDefaultValue: () => true)
};

var optionQuiet = new Option(new[] { "--quiet" }, "Set verbosity level to quiet")
{
    Argument = new Argument<bool>()
};

var rootCommand = new RootCommand {Description = "An .NET compression tool."};

rootCommand.AddOption(optionInputFiles);
rootCommand.AddOption(optionInputDirectory);
rootCommand.AddOption(optionOutputDirectory);
rootCommand.AddOption(optionOutputFiles);
rootCommand.AddOption(optionPattern);
rootCommand.AddOption(optionFormat);
rootCommand.AddOption(optionLevel);
rootCommand.AddOption(optionThreads);
rootCommand.AddOption(optionRecursive);
rootCommand.AddOption(optionQuiet);

rootCommand.Handler = CommandHandler.Create(static (FileCompressorSettings settings) => FileCompressor.Run(settings));

return await rootCommand.InvokeAsync(args);
