using System.Diagnostics;
using System.IO.Compression;
using DotNetCompress.Model;

namespace DotNetCompress;

internal static class App
{
    public static void Run(Settings settings)
    {
        var paths = GetPaths(settings);
        if (paths == null || paths.Count == 0)
        {
            return;
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

        var threads = settings.Threads;
        var jobs = GetJobs(settings, paths);

        Parallel.For(0, jobs.Count, new ParallelOptions {MaxDegreeOfParallelism = threads}, i =>
        {
            var job = jobs[i];
            try
            {
                Compress(job.InputPath, job.OutputPath, job.Format, job.CompressionLevel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{i}] Error: {job.InputPath}");
                Console.WriteLine(ex);
            }
        });

        sw.Stop();

        Console.WriteLine($"Done: {sw.Elapsed}");
    }

    private static List<FileInfo>? GetPaths(Settings settings)
    {
        var paths = new List<FileInfo>();

        if (settings.InputFiles is { })
        {
            paths.AddRange(settings.InputFiles);
        }

        if (settings.InputDirectory is { })
        {
            var directory = settings.InputDirectory;
            var patterns = settings.Pattern;
            if (patterns.Length == 0)
            {
                Console.WriteLine("Error: The pattern can not be empty.");
                return null;
            }

            foreach (var pattern in patterns)
            {
                GetFiles(directory, pattern, paths, settings.Recursive);
            }
        }

        return paths;
    }

    private static List<Job> GetJobs(Settings settings, List<FileInfo> paths)
    {
        var jobs = new List<Job>();

        for (var i = 0; i < paths.Count; i++)
        {
            var inputPath = paths[i];
            var outputFile = settings.OutputFiles?[i];
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

    private static void Compress(string inputPath, string outputPath, string format, CompressionLevel compressionLevel)
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
}
