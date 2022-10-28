using System.IO.Compression;

namespace DotNetCompress.Core;

internal record FileCompressorJob(string InputPath, string OutputPath, string Format, CompressionLevel CompressionLevel);
