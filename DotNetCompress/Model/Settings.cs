using System.IO.Compression;

namespace DotNetCompress.Model;

internal class Settings
{
    public FileInfo[]? InputFiles { get; set; } = null;
    public DirectoryInfo? InputDirectory { get; set; } = null;
    public FileInfo[]? OutputFiles { get; set; } = null;
    public DirectoryInfo? OutputDirectory { get; set; } = null;
    public string[] Pattern { get; set; } = { "*.*" };
    public string Format { get; set; } = "br";
    public CompressionLevel Level { get; set; } = CompressionLevel.SmallestSize;
    public int Threads { get; set; } = 1;
    public bool Recursive { get; set; } = true;
}
