﻿using System.Diagnostics;

namespace Lzwg.Tests;

public class Tests
{
    [OneTimeSetUp]
    public void Setup()
    {
        ConsoleTraceListener listener = new ConsoleTraceListener();
        Trace.Listeners.Add(listener);
    }
    
    [OneTimeTearDown]
    public void TearDown()
    {
        Trace.Flush();
        Trace.Listeners.Clear();
    }
    
    [TestCase(30)]
    [TestCase(128)]
    [TestCase(256)]
    [TestCase(1024)]
    [TestCase(int.MaxValue)]
    public void Test(int maxSize)
    {
        string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";
        
        List<int> compressed = Lzwg.Compress(text.ToCharArray(), text.ToHashSet(), maxSize);

        Console.WriteLine($"Output length: {compressed.Count}, Min symbol value: {compressed.Min()}, Max symbol value: {compressed.Max()}, Unique symbols: {compressed.Distinct().Count()}");
        
        string decompressed = new string(Lzwg.Decompress(compressed, text.ToHashSet(), maxSize).ToArray());
        
        Assert.That(decompressed, Is.EqualTo(text));
    }
    
    [TestCase(int.MaxValue, "aaaaaaaa")]
    [TestCase(int.MaxValue, "abaaacabaaccababbbbbbbcababababcbcbcbbbbbbabababccccccbbbbaaabbbbbccccccacbacbabcabc")]
    [TestCase(5, "abaaacabaaccababbbbbbbcababababcbcbcbbbbbbabababccccccbbbbaaabbbbbccccccacbacbabcabc")]
    [TestCase(10, "abaaacabaaccababbbbbbbcababababcbcbcbbbbbbabababccccccbbbbaaabbbbbccccccacbacbabcabc")]
    [TestCase(10, "abaaacabaaccababbbbbb")]
    [TestCase(15, "abaaacabaaccababbbbbbbcababababcbcbcbbbbbbabababccccccbbbbaaabbbbbccccccacbacbabcabc")]
    [TestCase(5, "babaca")]
    [TestCase(6, "ccabbbabccab")]
    [TestCase(5, "aaaaaaaa")]
    public void Test2(int maxSize, string text)
    {
        List<int> compressed = Lzwg.Compress(text.ToCharArray(), "abc".ToHashSet(), maxSize);
        
        Console.WriteLine($"Compressed: {string.Join(", ", compressed.Select(x => x))}\n");

        string decompressed = new string(Lzwg.Decompress(compressed, "abc".ToHashSet(), maxSize).ToArray());
        
        Assert.That(decompressed, Is.EqualTo(text));
    }
    
    [TestCase(3, "aaaaaaaa")]
    [TestCase(8, "abaaacabaaccababbbbbbbcababababcbcbcbbbbbbabababccccccbbbbaaabbbbbccccccacbacbabcabc")]
    [TestCase(6, "abaaacabaaccababbbbbb")]
    [TestCase(15, "abaaacabaaccababbbbbbbcababababcbcbcbbbbbbabababccccccbbbbaaabbbbbccccccacbacbabcabc")]
    public void TestWithReset(int resetAfter, string text)
    {
        List<int> compressed = Lzwg.Compress(text.ToCharArray(), "abc".ToHashSet(), int.MaxValue, resetAfter);
        
        Console.WriteLine($"Compressed: {string.Join(", ", compressed.Select(x => x))}\n");

        string decompressed = new string(Lzwg.Decompress(compressed, "abc".ToHashSet(), int.MaxValue, resetAfter).ToArray());
        
        Assert.That(decompressed, Is.EqualTo(text));
    }
}