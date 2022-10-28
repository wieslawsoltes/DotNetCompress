using System.Diagnostics;
using System.IO.Compression;

namespace DotNetCompress.Core;

public static class FileCompressor
{
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
                Console.WriteLine("Error: The number of the output files must match the number of the input files.");
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
                Console.WriteLine($"Error: {job.InputPath}");
                Console.WriteLine(ex);
            }
        });

        sw.Stop();

        if (!fileCompressorSettings.Quiet)
        {
            Console.WriteLine($"Done: {sw.Elapsed}");
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
                Console.WriteLine("Error: The pattern can not be empty.");
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
            Console.WriteLine($"Compressing: {outputPath}");
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
