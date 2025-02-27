﻿using System.Numerics;
using System.Runtime.InteropServices;

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

        uint huffmanMaxBits = 32;
        var huffmanSymbols = PackageMerge.ComputeHuffmanTablePackageMerge(compressed, huffmanMaxBits);
        int huffmanEncodedSize = compressed.Aggregate(0, (sizeBits, next) => sizeBits + huffmanSymbols[next].length) / 8;
        int huffmanTableSize = huffmanSymbols.Aggregate(0, (sizeBits, next) => sizeBits + next.length) / 8;
        Console.WriteLine($"- Huffman (max {huffmanMaxBits}, symbols: {huffmanSymbols.Length}) encoded size: {huffmanEncodedSize + huffmanTableSize} bytes ({huffmanEncodedSize} (payload) + {huffmanTableSize}(table))");

        var decompressedChars = Lzwg.Decompress(compressed, text.ToHashSet(), maxSize);
        ReadOnlySpan<char> span = CollectionsMarshal.AsSpan(decompressedChars);
        string decompressed = new string(span);
        
        Assert.That(decompressed, Is.EqualTo(text));
    }
}