﻿using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO.Compression;

var optionInputFiles = new Option(new[] { "--inputFiles", "-f" }, "The relative or absolute path to the input files")
{
    Argument = new Argument<FileInfo[]?>(getDefaultValue: () => null)
};

var optionInputDirectory = new Option(new[] { "--inputDirectory", "-d" }, "The relative or absolute path to the input directory")
{
    Argument = new Argument<DirectoryInfo?>(getDefaultValue: () => null)
};

var optionOutputDirectory = new Option(new[] { "--outputDirectory", "-o" }, "The relative or absolute path to the output directory")
{
    Argument = new Argument<DirectoryInfo?>(getDefaultValue: () => null)
};

var optionOutputFiles = new Option(new[] { "--outputFiles" }, "The relative or absolute path to the output files")
{
    Argument = new Argument<FileInfo[]?>(getDefaultValue: () => null)
};

var optionPattern = new Option(new[] { "--pattern", "-p" }, "The search string to match against the names of files in the input directory")
{
    Argument = new Argument<string?>(getDefaultValue: () => null)
};

var optionFormat = new Option(new[] { "--format" }, "The compression file format (br, gz)")
{
    Argument = new Argument<string>(getDefaultValue: () => "br")
};

var optionLevel = new Option(new[] { "--level", "-l" }, "The compression level (Optimal, Fastest, NoCompression, SmallestSize)")
{
    Argument = new Argument<CompressionLevel>(getDefaultValue: () => CompressionLevel.SmallestSize)
};

var rootCommand = new RootCommand
{
    Description = "An .NET compression tool."
};

rootCommand.AddOption(optionInputFiles);
rootCommand.AddOption(optionInputDirectory);
rootCommand.AddOption(optionOutputDirectory);
rootCommand.AddOption(optionOutputFiles);
rootCommand.AddOption(optionPattern);
rootCommand.AddOption(optionFormat);
rootCommand.AddOption(optionLevel);

rootCommand.Handler = CommandHandler.Create(static (Settings settings) => Run(settings));

return await rootCommand.InvokeAsync(args);

static void GetFiles(DirectoryInfo directory, string pattern, List<FileInfo> paths)
{
    var files = Directory.EnumerateFiles(directory.FullName, pattern);
    paths.AddRange(files.Select(path => new FileInfo(path)));
}

static void Run(Settings settings)
{
    var paths = new List<FileInfo>();

    if (settings.InputFiles is { })
    {
        foreach (var file in settings.InputFiles)
        {
            paths.Add(file);
        }
    }

    if (settings.InputDirectory is { })
    {
        var directory = settings.InputDirectory;
        var pattern = settings.Pattern;
        if (string.IsNullOrEmpty(pattern))
        {
            Console.WriteLine("Error: The pattern can not be null or empty.");
        }
        else
        {
            GetFiles(directory, pattern, paths);
        }
    }

    if (settings.OutputFiles is { })
    {
        if (paths.Count > 0 && paths.Count != settings.OutputFiles.Length)
        {
            Console.WriteLine("Error: The number of the output files must match the number of the input files.");
            return;
        }
    }

    if (settings.OutputDirectory is { } && !string.IsNullOrEmpty(settings.OutputDirectory.FullName))
    {
        if (!Directory.Exists(settings.OutputDirectory.FullName))
        {
            Directory.CreateDirectory(settings.OutputDirectory.FullName);
        }
    }

    var sw = Stopwatch.StartNew();

    int processed = 0;

    for (var i = 0; i < paths.Count; i++)
    {
        var inputPath = paths[i];
        var outputFile = settings.OutputFiles?[i];
        try
        {
            string outputPath;
            
            if (outputFile is { })
            {
                outputPath = outputFile.FullName;
            }
            else
            {
                outputPath = inputPath.FullName + "." + settings.Format.ToLower();
                if (settings.OutputDirectory is { } && !string.IsNullOrEmpty(settings.OutputDirectory.FullName))
                {
                    outputPath = Path.Combine(settings.OutputDirectory.FullName, Path.GetFileName(outputPath));
                }
            }

            var currentDirectoryName = Path.GetDirectoryName(inputPath.FullName);
            if (currentDirectoryName is { })
            {
                Directory.SetCurrentDirectory(currentDirectoryName);
            }

            Compress(inputPath.FullName, outputPath, settings.Format, settings.Level);
            processed++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{i}] Error: {inputPath}");
            Console.WriteLine(ex);
        }
    }

    sw.Stop();

    if (paths.Count > 0)
    {
        Console.WriteLine($"Done: {sw.Elapsed} ({processed}/{paths.Count})");
    }
}

static void Compress(string inputPath, string outputPath, string format, CompressionLevel compressionLevel)
{
    Console.WriteLine($"Compressing: {outputPath}");

    switch (format.ToLower())
    {
        case "br":
        {
            CompressBrotli(inputPath, outputPath, compressionLevel);
            break;
        }
        case "gz":
        {
            CompressGZip(inputPath, outputPath, compressionLevel);
            break;
        }
    }
}

static void CompressBrotli(string inPath, string outPath, CompressionLevel compressionLevel)
{
    using var input = File.OpenRead(inPath);
    using var output = File.Create(outPath);
    using var compressor = new BrotliStream(output, compressionLevel);
    input.CopyTo(compressor);
}

static void CompressGZip(string inPath, string outPath, CompressionLevel compressionLevel)
{
    using var input = File.OpenRead(inPath);
    using var output = File.Create(outPath);
    using var compressor = new GZipStream(output, compressionLevel);
    input.CopyTo(compressor);
}

class Settings
{
    public FileInfo[]? InputFiles { get; set; } = null;
    public DirectoryInfo? InputDirectory { get; set; } = null;
    public FileInfo[]? OutputFiles { get; set; } = null;
    public DirectoryInfo? OutputDirectory { get; set; } = null;
    public string Pattern { get; set; } = "*.*";
    public string Format { get; set; } = "br";
    public CompressionLevel Level { get; set; } = CompressionLevel.SmallestSize;
}
