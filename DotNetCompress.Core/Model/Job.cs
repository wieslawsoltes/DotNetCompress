using System.IO.Compression;

namespace DotNetCompress.Core.Model;

public record Job(string InputPath, string OutputPath, string Format, CompressionLevel CompressionLevel);
