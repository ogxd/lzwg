﻿namespace Lzwg.Tests;

public class Tests
{
    [TestCase(30)]
    [TestCase(128)]
    [TestCase(256)]
    [TestCase(1024)]
    [TestCase(int.MaxValue)]
    public void Test(int maxSize)
    {
        string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";
        
        LzwgCompressor<char> compressor = new(maxSize, text.ToCharArray());

        List<int> compressed = compressor.Compress(text.ToCharArray());

        Console.WriteLine($"Ints: {compressed.Count}, Max: {compressed.Max()}");
        
        LzwgDecompressor<char> decompressor = new(maxSize, text.ToCharArray());
        
        string decompressed = new string(decompressor.Decompress(compressed).ToArray());
        
        Assert.That(decompressed, Is.EqualTo(text));
    }
    
    [TestCase(int.MaxValue, "aaaaaaaa")]
    [TestCase(int.MaxValue, "abaaacabaaccababbbbbbbcababababcbcbcbbbbbbabababccccccbbbbaaabbbbbccccccacbacbabcabc")]
    [TestCase(5, "abaaacabaaccababbbbbbbcababababcbcbcbbbbbbabababccccccbbbbaaabbbbbccccccacbacbabcabc")]
    [TestCase(10, "abaaacabaaccababbbbbbbcababababcbcbcbbbbbbabababccccccbbbbaaabbbbbccccccacbacbabcabc")]
    [TestCase(15, "abaaacabaaccababbbbbbbcababababcbcbcbbbbbbabababccccccbbbbaaabbbbbccccccacbacbabcabc")]
    [TestCase(5, "babaca")]
    [TestCase(6, "ccabbbabccab")]
    [TestCase(5, "aaaaaaaa")]
    public void Test2(int maxSize, string text)
    {
        LzwgCompressor<char> compressor = new(maxSize, "abc".ToCharArray());

        List<int> compressed = compressor.Compress(text.ToCharArray());
        
        Console.WriteLine($"Compressed: {string.Join(", ", compressed.Select(x => x))}");
        Console.WriteLine("---");
        
        LzwgDecompressor<char> decompressor = new(maxSize, "abc".ToCharArray());
        
        string decompressed = new string(decompressor.Decompress(compressed).ToArray());
        
        Assert.That(decompressed, Is.EqualTo(text));
    }
}