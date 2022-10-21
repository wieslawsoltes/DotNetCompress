using System.IO.Compression;

namespace DotNetCompress.Model;

internal record Job(string InputPath, string OutputPath, string Format, CompressionLevel CompressionLevel);
