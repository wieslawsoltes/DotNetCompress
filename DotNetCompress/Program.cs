using System.CommandLine;
using System.CommandLine.Binding;
using System.Diagnostics;
using System.IO.Compression;

var optionInputFiles = new Option<FileInfo[]?>(
    new[] {"--inputFiles", "-f"},
    () => null,
    "The relative or absolute path to the input files");

var optionInputDirectory = new Option<DirectoryInfo?>(
    new[] {"--inputDirectory", "-d"},
    () => null,
    "The relative or absolute path to the input directory");

var optionOutputDirectory = new Option<DirectoryInfo?>(
    new[] {"--outputDirectory", "-o"},
    () => null,
    "The relative or absolute path to the output directory");

var optionOutputFiles = new Option<FileInfo[]?>(
    new[] {"--outputFiles"},
    () => null,
    "The relative or absolute path to the output files");

var optionPattern = new Option<string?>(
    new[] {"--pattern", "-p"},
    () => "*.*",
    "The search string to match against the names of files in the input directory");

var optionFormat = new Option<string?>(
    new[] {"--format"},
    () => "br",
    "The compression file format (br, gz)");

var optionLevel = new Option<CompressionLevel>(
    new[] {"--level", "-l"},
    () => CompressionLevel.SmallestSize,
    "The compression level (Optimal, Fastest, NoCompression, SmallestSize)");

var optionThreads = new Option<int>(
    new[] {"--threads", "-t"},
    () => 1,
    "The number of parallel job threads");

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
rootCommand.AddOption(optionThreads);

rootCommand.SetHandler(
    Run, 
    new SettingsBinder(
        optionInputFiles, 
        optionInputDirectory,
        optionOutputDirectory,
        optionOutputFiles,
        optionPattern,
        optionFormat,
        optionLevel,
        optionThreads));

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

    if (settings.Format is null)
    {
        Console.WriteLine("Error: The format can not be null or empty.");
        return;
    }
    
    var sw = Stopwatch.StartNew();

    var threads = settings.Threads;
    var jobs = new List<Job>();

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

            jobs.Add(new Job(inputPath.FullName, outputPath, settings.Format, settings.Level));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{i}] Error: {inputPath}");
            Console.WriteLine(ex);
        }
    }

    Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
    {
        var job = jobs[i];
        Compress(job.InputPath, job.OutputPath, job.Format, job.CompressionLevel);
    });

    sw.Stop();

    if (paths.Count > 0)
    {
        Console.WriteLine($"Done: {sw.Elapsed})");
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
    public FileInfo[]? InputFiles { get; set; }
    public DirectoryInfo? InputDirectory { get; set; }
    public FileInfo[]? OutputFiles { get; set; }
    public DirectoryInfo? OutputDirectory { get; set; }
    public string? Pattern { get; set; } = "*.*";
    public string? Format { get; set; } = "br";
    public CompressionLevel Level { get; set; } = CompressionLevel.SmallestSize;
    public int Threads { get; set; } = 1;
}

class SettingsBinder : BinderBase<Settings>
{
    private readonly Option<FileInfo[]?> _inputFiles;
    private readonly Option<DirectoryInfo?> _inputDirectory;
    private readonly Option<FileInfo[]?> _outputFiles;
    private readonly Option<DirectoryInfo?> _outputDirectory;
    private readonly Option<string?> _pattern;
    private readonly Option<string?> _format;
    private readonly Option<CompressionLevel> _level;
    private readonly Option<int> _threads;

    public SettingsBinder(
        Option<FileInfo[]?> inputFiles,
        Option<DirectoryInfo?> inputDirectory,
        Option<DirectoryInfo?> outputDirectory,
        Option<FileInfo[]?> outputFiles,
        Option<string?> pattern,
        Option<string?> format,
        Option<CompressionLevel> level,
        Option<int> threads)
    {
        _inputFiles = inputFiles;
        _inputDirectory = inputDirectory;
        _outputDirectory = outputDirectory;
        _outputFiles = outputFiles;
        _pattern = pattern;
        _format = format;
        _level = level;
        _threads = threads;
    }

    protected override Settings GetBoundValue(BindingContext bindingContext) =>
        new Settings
        {
            InputFiles = bindingContext.ParseResult.GetValueForOption(_inputFiles),
            InputDirectory = bindingContext.ParseResult.GetValueForOption(_inputDirectory),
            OutputDirectory = bindingContext.ParseResult.GetValueForOption(_outputDirectory),
            OutputFiles = bindingContext.ParseResult.GetValueForOption(_outputFiles),
            Pattern = bindingContext.ParseResult.GetValueForOption(_pattern),
            Format = bindingContext.ParseResult.GetValueForOption(_format),
            Level = bindingContext.ParseResult.GetValueForOption(_level),
            Threads = bindingContext.ParseResult.GetValueForOption(_threads),
        };
}

record Job(string InputPath, string OutputPath, string Format, CompressionLevel CompressionLevel);
