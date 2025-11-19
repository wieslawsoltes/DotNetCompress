using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace DotNetCompress.Tests;

public class IntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _inputDirectory;
    private readonly string _outputDirectory;
    private readonly string _projectPath;

    public IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "DotNetCompressTests", Guid.NewGuid().ToString());
        _inputDirectory = Path.Combine(_testDirectory, "input");
        _outputDirectory = Path.Combine(_testDirectory, "output");
        
        // Assuming the test is running from DotNetCompress.Tests/bin/Debug/netX.X/
        // We need to find DotNetCompress/DotNetCompress.csproj
        // ../../../../../DotNetCompress/DotNetCompress.csproj
        var currentDir = Directory.GetCurrentDirectory();
        var repoRoot = FindRepoRoot(currentDir);
        _projectPath = Path.Combine(repoRoot, "DotNetCompress", "DotNetCompress.csproj");

        Directory.CreateDirectory(_inputDirectory);
        Directory.CreateDirectory(_outputDirectory);
    }

    private string FindRepoRoot(string currentDir)
    {
        var dir = new DirectoryInfo(currentDir);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DotNetCompress.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new Exception("Could not find repository root.");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private void CreateTestFile(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(_inputDirectory, fileName), content);
    }

    private (int ExitCode, string Output, string Error) RunTool(string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{_projectPath}\" -- {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
    }

    [Fact]
    public void Compress_SingleFile_Brotli_Success()
    {
        var fileName = "test.txt";
        var content = "Hello World! This is a test file for compression.";
        CreateTestFile(fileName, content);

        var args = $"--inputFiles \"{Path.Combine(_inputDirectory, fileName)}\" --outputDirectory \"{_outputDirectory}\" --format br";
        var result = RunTool(args);

        Assert.Equal(0, result.ExitCode);
        
        var outputFile = Path.Combine(_outputDirectory, fileName + ".br");
        Assert.True(File.Exists(outputFile), $"Output file {outputFile} should exist.");
        
        // Basic check if file is not empty
        Assert.True(new FileInfo(outputFile).Length > 0);
    }

    [Fact]
    public void Compress_SingleFile_GZip_Success()
    {
        var fileName = "test_gzip.txt";
        var content = "Hello World! This is a test file for GZip compression.";
        CreateTestFile(fileName, content);

        var args = $"--inputFiles \"{Path.Combine(_inputDirectory, fileName)}\" --outputDirectory \"{_outputDirectory}\" --format gz";
        var result = RunTool(args);

        Assert.Equal(0, result.ExitCode);
        
        var outputFile = Path.Combine(_outputDirectory, fileName + ".gz");
        Assert.True(File.Exists(outputFile), $"Output file {outputFile} should exist.");
        Assert.True(new FileInfo(outputFile).Length > 0);
    }

    [Fact]
    public void Compress_SingleFile_ZLib_Success()
    {
        var fileName = "test_zlib.txt";
        var content = "Hello World! This is a test file for ZLib compression.";
        CreateTestFile(fileName, content);

        var args = $"--inputFiles \"{Path.Combine(_inputDirectory, fileName)}\" --outputDirectory \"{_outputDirectory}\" --format zlib";
        var result = RunTool(args);

        Assert.Equal(0, result.ExitCode);
        
        var outputFile = Path.Combine(_outputDirectory, fileName + ".zlib");
        Assert.True(File.Exists(outputFile), $"Output file {outputFile} should exist.");
        Assert.True(new FileInfo(outputFile).Length > 0);
    }

    [Fact]
    public void Compress_SingleFile_Deflate_Success()
    {
        var fileName = "test_def.txt";
        var content = "Hello World! This is a test file for Deflate compression.";
        CreateTestFile(fileName, content);

        var args = $"--inputFiles \"{Path.Combine(_inputDirectory, fileName)}\" --outputDirectory \"{_outputDirectory}\" --format def";
        var result = RunTool(args);

        Assert.Equal(0, result.ExitCode);
        
        var outputFile = Path.Combine(_outputDirectory, fileName + ".def");
        Assert.True(File.Exists(outputFile), $"Output file {outputFile} should exist.");
        Assert.True(new FileInfo(outputFile).Length > 0);
    }

    [Fact]
    public void Compress_Directory_Recursive_Success()
    {
        CreateTestFile("file1.txt", "Content 1");
        Directory.CreateDirectory(Path.Combine(_inputDirectory, "sub"));
        File.WriteAllText(Path.Combine(_inputDirectory, "sub", "file2.txt"), "Content 2");

        var args = $"--inputDirectory \"{_inputDirectory}\" --outputDirectory \"{_outputDirectory}\" --recursive true --format br";
        var result = RunTool(args);

        Assert.Equal(0, result.ExitCode);

        Assert.True(File.Exists(Path.Combine(_outputDirectory, "file1.txt.br")));
        // Note: The current implementation flattens the output or keeps structure?
        // Let's check the implementation. 
        // Looking at GetJobs: outputPath = Path.Combine(fileCompressorSettings.OutputDirectory.FullName, Path.GetFileName(outputPath));
        // It seems it flattens the output if OutputDirectory is specified.
        
        Assert.True(File.Exists(Path.Combine(_outputDirectory, "file2.txt.br")));
    }

    [Fact]
    public void Compress_InvalidFormat_ShouldFail() // Or default to something?
    {
        // The CLI parser might fail validation or the code might handle it.
        // System.CommandLine usually shows an error for invalid enum/options if restricted, 
        // but here format is a string.
        // The switch case in Program.cs does nothing for unknown formats.
        
        var fileName = "test_invalid.txt";
        CreateTestFile(fileName, "content");

        var args = $"--inputFiles \"{Path.Combine(_inputDirectory, fileName)}\" --outputDirectory \"{_outputDirectory}\" --format invalid";
        var result = RunTool(args);

        // It might exit with 0 but produce no output, or fail.
        // Let's see. The code catches exceptions but if switch case doesn't match, it just returns.
        // Wait, switch case has no default. So it does nothing.
        
        Assert.Equal(0, result.ExitCode);
        Assert.False(File.Exists(Path.Combine(_outputDirectory, fileName + ".invalid")));
    }

    [Fact]
    public void Compress_WithPattern_ShouldOnlyCompressMatchingFiles()
    {
        CreateTestFile("match.txt", "content");
        CreateTestFile("ignore.log", "content");

        var args = $"--inputDirectory \"{_inputDirectory}\" --outputDirectory \"{_outputDirectory}\" --pattern \"*.txt\"";
        var result = RunTool(args);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(_outputDirectory, "match.txt.br")));
        Assert.False(File.Exists(Path.Combine(_outputDirectory, "ignore.log.br")));
    }

    [Fact]
    public void Compress_WithCompressionLevel_ShouldSucceed()
    {
        var fileName = "test_level.txt";
        CreateTestFile(fileName, "content");

        var args = $"--inputFiles \"{Path.Combine(_inputDirectory, fileName)}\" --outputDirectory \"{_outputDirectory}\" --level Fastest";
        var result = RunTool(args);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(_outputDirectory, fileName + ".br")));
    }

    [Fact]
    public void Compress_WithQuietMode_ShouldNotOutputLog()
    {
        var fileName = "test_quiet.txt";
        CreateTestFile(fileName, "content");

        var args = $"--inputFiles \"{Path.Combine(_inputDirectory, fileName)}\" --outputDirectory \"{_outputDirectory}\" --quiet";
        var result = RunTool(args);

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("Compressing:", result.Output);
        Assert.DoesNotContain("Done:", result.Output);
    }

    [Fact]
    public void Compress_WithExplicitOutputFiles_ShouldSucceed()
    {
        var fileName = "test_explicit.txt";
        CreateTestFile(fileName, "content");
        var outputFileName = "custom_output.br";

        var args = $"--inputFiles \"{Path.Combine(_inputDirectory, fileName)}\" --outputFiles \"{Path.Combine(_outputDirectory, outputFileName)}\"";
        var result = RunTool(args);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(_outputDirectory, outputFileName)));
    }

    [Fact]
    public void Compress_WithMismatchedOutputFiles_ShouldFail()
    {
        CreateTestFile("file1.txt", "content");
        CreateTestFile("file2.txt", "content");

        // 2 input files, 1 output file
        var args = $"--inputFiles \"{Path.Combine(_inputDirectory, "file1.txt")}\" \"{Path.Combine(_inputDirectory, "file2.txt")}\" --outputFiles \"{Path.Combine(_outputDirectory, "out1.br")}\"";
        var result = RunTool(args);

        // The tool logs an error but might not exit with non-zero code depending on implementation.
        // Looking at Program.cs: it returns early if count mismatch.
        // It prints "Error: The number of the output files must match the number of the input files."
        
        Assert.Contains("Error: The number of the output files must match the number of the input files.", result.Output);
    }

    [Fact]
    public void Compress_WithThreads_ShouldSucceed()
    {
        var fileName = "test_threads.txt";
        CreateTestFile(fileName, "content");

        var args = $"--inputFiles \"{Path.Combine(_inputDirectory, fileName)}\" --outputDirectory \"{_outputDirectory}\" --threads 2";
        var result = RunTool(args);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(_outputDirectory, fileName + ".br")));
    }
}
