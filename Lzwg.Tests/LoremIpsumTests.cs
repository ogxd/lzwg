using System.Numerics;

namespace Lzwg.Tests;

public class LoremIpsumTests
{
    [TestCase(64)]
    [TestCase(128)]
    [TestCase(256)]
    [TestCase(1024)]
    [TestCase(2048)]
    [TestCase(4096)]
    [TestCase(8192)]
    [TestCase(16384)]
    [TestCase(int.MaxValue)]
    public void Test(int maxSize)
    {
        // Load text from file
        string text = File.ReadAllText("LoremIpsum.txt");
        
        List<int> compressed = Lzwg.Compress(text.ToCharArray(), text.ToHashSet(), maxSize);
        
        Console.WriteLine("Output:");
        Console.WriteLine($"- Length: {compressed.Count}");
        Console.WriteLine($"- Min symbol value: {compressed.Min()}");
        Console.WriteLine($"- Max symbol value: {compressed.Max()}");
        Console.WriteLine($"- Unique symbols: {compressed.Distinct().Count()}");
        
        Console.WriteLine("Compression:");
        Console.WriteLine($"- Original size: {text.Length} bytes");
        Console.WriteLine($"- Leb128 encoded size: {compressed.Aggregate(0, (sizeBits, next) => {
            EncodingUtils.EncodeLeb128((ulong)next, out int bits);
            return sizeBits + bits;
        }) / 8} bytes");
        int countBits = BitOperations.Log2((uint)BitOperations.Log2((uint)maxSize) + 1) + 1;
        Console.WriteLine($"- Prefix ({countBits}) encoded size: {compressed.Aggregate(0, (sizeBits, next) => {
            EncodingUtils.EncodePrefix((uint)next, countBits, out int bits);
            return sizeBits + bits;
        }) / 8} bytes");
        // Todo: Check with huffman encoding
        
        string decompressed = new string(Lzwg.Decompress(compressed, text.ToHashSet(), maxSize).ToArray());
        
        Assert.That(decompressed, Is.EqualTo(text));
    }
}