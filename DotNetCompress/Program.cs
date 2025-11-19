using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO.Compression;

await CreateRootCommand().Parse(args).InvokeAsync();

RootCommand CreateRootCommand()
{
    var optionInputFiles = new Option<FileInfo[]?>("--inputFiles", "-f")
    {
        Description = "The relative or absolute path to the input files",
        AllowMultipleArgumentsPerToken = true
    };

    var optionInputDirectory = new Option<DirectoryInfo?>("--inputDirectory", "-d")
    {
        Description = "The relative or absolute path to the input directory"
    };

    var optionOutputDirectory = new Option<DirectoryInfo?>("--outputDirectory", "-o")
    {
        Description = "The relative or absolute path to the output directory"
    };

    var optionOutputFiles = new Option<FileInfo[]?>("--outputFiles")
    {
        Description = "The relative or absolute path to the output files",
        AllowMultipleArgumentsPerToken = true
    };

    var optionPattern = new Option<string[]>("--pattern", "-p")
    {
        Description = "The search string to match against the names of files in the input directory",
        DefaultValueFactory = _ => new[] { "*.*" },
        AllowMultipleArgumentsPerToken = true
    };

    var optionFormat = new Option<string>("--format")
    {
        Description = "The compression file format (br, gz, zlib, def)",
        DefaultValueFactory = _ => "br"
    };

    var optionLevel = new Option<CompressionLevel>("--level", "-l")
    {
        Description = "The compression level (Optimal, Fastest, NoCompression, SmallestSize)",
        DefaultValueFactory = _ => CompressionLevel.SmallestSize
    };

    var optionThreads = new Option<int>("--threads", "-t")
    {
        Description = "The number of parallel job threads",
        DefaultValueFactory = _ => 1
    };

    var optionRecursive = new Option<bool>("--recursive", "-r")
    {
        Description = "Recurse into subdirectories of input directory search",
        DefaultValueFactory = _ => true
    };

    var optionQuiet = new Option<bool>("--quiet")
    {
        Description = "Set verbosity level to quiet"
    };

    var rootCommand = new RootCommand("An .NET compression tool.");

    rootCommand.Options.Add(optionInputFiles);
    rootCommand.Options.Add(optionInputDirectory);
    rootCommand.Options.Add(optionOutputDirectory);
    rootCommand.Options.Add(optionOutputFiles);
    rootCommand.Options.Add(optionPattern);
    rootCommand.Options.Add(optionFormat);
    rootCommand.Options.Add(optionLevel);
    rootCommand.Options.Add(optionThreads);
    rootCommand.Options.Add(optionRecursive);
    rootCommand.Options.Add(optionQuiet);

    rootCommand.SetAction(parseResult =>
    {
        var settings = new FileCompressorSettings
        {
            InputFiles = parseResult.GetValue(optionInputFiles),
            InputDirectory = parseResult.GetValue(optionInputDirectory),
            OutputDirectory = parseResult.GetValue(optionOutputDirectory),
            OutputFiles = parseResult.GetValue(optionOutputFiles),
            Pattern = parseResult.GetValue(optionPattern)!,
            Format = parseResult.GetValue(optionFormat)!,
            Level = parseResult.GetValue(optionLevel),
            Threads = parseResult.GetValue(optionThreads),
            Recursive = parseResult.GetValue(optionRecursive),
            Quiet = parseResult.GetValue(optionQuiet)
        };
        FileCompressor.Run(settings);
    });

    return rootCommand;
}

internal record FileCompressorJob(
    string InputPath, 
    string OutputPath, 
    string Format, 
    CompressionLevel CompressionLevel,
    bool Quiet);

internal class FileCompressorSettings
{
    public FileInfo[]? InputFiles { get; set; } = null;
    public DirectoryInfo? InputDirectory { get; set; } = null;
    public FileInfo[]? OutputFiles { get; set; } = null;
    public DirectoryInfo? OutputDirectory { get; set; } = null;
    public string[] Pattern { get; set; } = {"*.*"};
    public string Format { get; set; } = "br";
    public CompressionLevel Level { get; set; } = CompressionLevel.SmallestSize;
    public int Threads { get; set; } = 1;
    public bool Recursive { get; set; } = true;
    public bool Quiet { get; set; } = false;
}

internal static class FileCompressor
{
    private static readonly Action<object>? s_log = Console.WriteLine;

    public static void Run(FileCompressorSettings fileCompressorSettings)
    {
        var paths = GetPaths(fileCompressorSettings);
        if (paths == null || paths.Count == 0)
        {
            return;
        }

        if (fileCompressorSettings.OutputFiles is { Length: > 0 })
        {
            if (paths.Count > 0 && paths.Count != fileCompressorSettings.OutputFiles.Length)
            {
                if (!fileCompressorSettings.Quiet)
                {
                    s_log?.Invoke("Error: The number of the output files must match the number of the input files.");
                }

                return;
            }
        }

        if (fileCompressorSettings.OutputDirectory is { } && !string.IsNullOrEmpty(fileCompressorSettings.OutputDirectory.FullName))
        {
            if (!Directory.Exists(fileCompressorSettings.OutputDirectory.FullName))
            {
                Directory.CreateDirectory(fileCompressorSettings.OutputDirectory.FullName);
            }
        }

        var sw = Stopwatch.StartNew();

        var jobs = GetJobs(fileCompressorSettings, paths);

        Parallel.For(0, jobs.Count, new ParallelOptions {MaxDegreeOfParallelism = fileCompressorSettings.Threads}, 
            i =>
            {
                var job = jobs[i];
                try
                {
                    if (!job.Quiet)
                    {
                        s_log?.Invoke($"Compressing: {job.OutputPath}");
                    }

                    Compress(job.InputPath, job.OutputPath, job.Format, job.CompressionLevel);
                }
                catch (Exception ex)
                {
                    if (!job.Quiet)
                    {
                        s_log?.Invoke($"Error: {job.InputPath}");
                        s_log?.Invoke(ex);
                    }
                }
            });

        sw.Stop();

        if (!fileCompressorSettings.Quiet)
        {
            s_log?.Invoke($"Done: {sw.Elapsed}");
        }
    }

    private static List<FileInfo>? GetPaths(FileCompressorSettings fileCompressorSettings)
    {
        var paths = new List<FileInfo>();

        if (fileCompressorSettings.InputFiles is { })
        {
            paths.AddRange(fileCompressorSettings.InputFiles);
        }

        if (fileCompressorSettings.InputDirectory is { })
        {
            var directory = fileCompressorSettings.InputDirectory;
            var patterns = fileCompressorSettings.Pattern;
            if (patterns.Length == 0)
            {
                if (!fileCompressorSettings.Quiet)
                {
                    s_log?.Invoke("Error: The pattern can not be empty.");
                }
                return null;
            }

            foreach (var pattern in patterns)
            {
                GetFiles(directory, pattern, paths, fileCompressorSettings.Recursive);
            }
        }

        return paths;
    }

    private static List<FileCompressorJob> GetJobs(FileCompressorSettings fileCompressorSettings, List<FileInfo> paths)
    {
        var jobs = new List<FileCompressorJob>();

        for (var i = 0; i < paths.Count; i++)
        {
            var inputPath = paths[i];
            FileInfo? outputFile = null;
            if (fileCompressorSettings.OutputFiles is { Length: > 0 } && i < fileCompressorSettings.OutputFiles.Length)
            {
                outputFile = fileCompressorSettings.OutputFiles[i];
            }
            string outputPath;

            if (outputFile is { })
            {
                outputPath = outputFile.FullName;
            }
            else
            {
                outputPath = inputPath.FullName + "." + fileCompressorSettings.Format.ToLower();
                if (fileCompressorSettings.OutputDirectory is { } && !string.IsNullOrEmpty(fileCompressorSettings.OutputDirectory.FullName))
                {
                    outputPath = Path.Combine(fileCompressorSettings.OutputDirectory.FullName, Path.GetFileName(outputPath));
                }
            }

            jobs.Add(new FileCompressorJob(inputPath.FullName, outputPath, fileCompressorSettings.Format, fileCompressorSettings.Level, fileCompressorSettings.Quiet));
        }

        return jobs;
    }

    private static void GetFiles(DirectoryInfo directory, string pattern, List<FileInfo> paths, bool recursive)
    {
        var files = Directory.EnumerateFiles(directory.FullName, pattern);

        paths.AddRange(files.Select(path => new FileInfo(path)));

        if (!recursive)
        {
            return;
        }

        foreach (var subDirectory in directory.EnumerateDirectories())
        {
            GetFiles(subDirectory, pattern, paths, recursive);
        }
    }

    private static void CompressBrotli(string inPath, string outPath, CompressionLevel compressionLevel)
    {
        using var input = File.OpenRead(inPath);
        using var output = File.Create(outPath);
        using var compressor = new BrotliStream(output, compressionLevel);
        input.CopyTo(compressor);
    }

    private static void CompressGZip(string inPath, string outPath, CompressionLevel compressionLevel)
    {
        using var input = File.OpenRead(inPath);
        using var output = File.Create(outPath);
        using var compressor = new GZipStream(output, compressionLevel);
        input.CopyTo(compressor);
    }

    private static void CompressZLib(string inPath, string outPath, CompressionLevel compressionLevel)
    {
        using var input = File.OpenRead(inPath);
        using var output = File.Create(outPath);
        using var compressor = new ZLibStream(output, compressionLevel);
        input.CopyTo(compressor);
    }

    private static void CompressDeflate(string inPath, string outPath, CompressionLevel compressionLevel)
    {
        using var input = File.OpenRead(inPath);
        using var output = File.Create(outPath);
        using var compressor = new DeflateStream(output, compressionLevel);
        input.CopyTo(compressor);
    }

    private static void Compress(string inputPath, string outputPath, string format, CompressionLevel compressionLevel)
    {
        switch (format.ToLower())
        {
            case "br":
                CompressBrotli(inputPath, outputPath, compressionLevel);
                break;
            case "gz":
                CompressGZip(inputPath, outputPath, compressionLevel);
                break;
            case "zlib":
                CompressZLib(inputPath, outputPath, compressionLevel);
                break;
            case "def":
            case "deflate":
                CompressDeflate(inputPath, outputPath, compressionLevel);
                break;
        }
    }
}
