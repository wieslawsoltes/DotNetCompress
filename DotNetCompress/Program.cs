using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO.Compression;

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

record FileCompressorJob(
    string InputPath, 
    string OutputPath, 
    string Format, 
    CompressionLevel CompressionLevel);

class FileCompressorSettings
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

static class FileCompressor
{
    public static Action<object>? Log = Console.WriteLine;

    public static void Run(FileCompressorSettings fileCompressorSettings)
    {
        var paths = GetPaths(fileCompressorSettings);
        if (paths == null || paths.Count == 0)
        {
            return;
        }

        if (fileCompressorSettings.OutputFiles is { })
        {
            if (paths.Count > 0 && paths.Count != fileCompressorSettings.OutputFiles.Length)
            {
                Log?.Invoke("Error: The number of the output files must match the number of the input files.");
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

        var quiet = fileCompressorSettings.Quiet;
        var threads = fileCompressorSettings.Threads;
        var jobs = GetJobs(fileCompressorSettings, paths);

        Parallel.For(0, jobs.Count, new ParallelOptions {MaxDegreeOfParallelism = threads}, i =>
        {
            var job = jobs[i];
            try
            {
                Compress(job.InputPath, job.OutputPath, job.Format, job.CompressionLevel, quiet);
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Error: {job.InputPath}");
                Log?.Invoke(ex);
            }
        });

        sw.Stop();

        if (!fileCompressorSettings.Quiet)
        {
            Log?.Invoke($"Done: {sw.Elapsed}");
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
                Log?.Invoke("Error: The pattern can not be empty.");
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
            var outputFile = fileCompressorSettings.OutputFiles?[i];
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

            jobs.Add(new FileCompressorJob(inputPath.FullName, outputPath, fileCompressorSettings.Format, fileCompressorSettings.Level));
        }

        return jobs;
    }

    private static void GetFiles(DirectoryInfo directory, string pattern, List<FileInfo> paths, bool recursive)
    {
        var files = Directory.EnumerateFiles(directory.FullName, pattern);

        paths.AddRange(files.Select(path => new FileInfo(path)));

        if (recursive)
        {
            foreach (var subDirectory in directory.EnumerateDirectories())
            {
                GetFiles(subDirectory, pattern, paths, recursive);
            }
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

    private static void Compress(string inputPath, string outputPath, string format, CompressionLevel compressionLevel, bool quiet)
    {
        if (!quiet)
        {
            Log?.Invoke($"Compressing: {outputPath}");
        }

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
}
