using System.IO.Compression;

if (args.Length != 2)
{
    Console.WriteLine("Usage: DotNetCompress <path> <pattern>");
    return;
}

var path = args[0];
var pattern = args[1];

var files = Directory.GetFiles(path, pattern);

foreach (var file in files)
{
    var inPath = file;
    var outPath = inPath + ".br";
    Console.WriteLine($"Compressing: {outPath}");
    CompressBrotli(inPath, outPath);
}

foreach (var file in files)
{
    var inPath = file;
    var outPath = inPath + ".br";
    Console.WriteLine($"Compressing: {outPath}");
    CompressGZip(inPath, outPath);
}

void CompressBrotli(string inPath, string outPath)
{
    using var input = File.OpenRead(inPath);
    using var output = File.Create(outPath);
    using var compressor = new BrotliStream(output, CompressionLevel.SmallestSize);
    input.CopyTo(compressor);
}

void CompressGZip(string inPath, string outPath)
{
    using var input = File.OpenRead(inPath);
    using var output = File.Create(outPath);
    using var compressor = new GZipStream(output, CompressionLevel.SmallestSize);
    input.CopyTo(compressor);
}
