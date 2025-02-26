using System.Numerics;
using System.Runtime.InteropServices;
using Lzwg.Tests;

namespace Lzwg.Benchmarks;

public class Serie
{
    public List<double> X = new();
    public List<double> Y = new();
}

public static class Plots
{
    public static void DoWork()
    {
        // Load text from file
        var text = File.ReadAllText("LoremIpsum.txt").ToCharArray();
        var series = Compute(text);
        
        ScottPlot.Plot myPlot = new();
        foreach (var (name, serie) in series)
        {
            var scatter = myPlot.Add.Scatter(serie.X.ToArray(), serie.Y.ToArray());
            scatter.LegendText = name;
        }
        myPlot.ShowLegend();
        myPlot.Title("LZWG Compression on Lorem Ipsum");
        myPlot.XLabel("Maximum dictionary size");
        myPlot.YLabel("Compression ratio (higher is better)");
        myPlot.SavePng("../../../bench.png", 800, 600);
    }

    // Y = Compression ratio
    // X = Max size
    private static Dictionary<string, Serie> Compute<T>(IReadOnlyList<T> originalData)
    {
        Dictionary<string, Serie> series = new();
        series["Leb128"] = new Serie();
        series["Prefix"] = new Serie();
        series["Huffman"] = new Serie();
        
        for (double i = 128; i < 16000; i += 128)
        {
            int maxSize = (int)i;
            List<int> compressed = Lzwg.Compress<T>(originalData.ToArray(), originalData.ToHashSet(), maxSize);
            
            int originalSize = originalData.Count * Marshal.SizeOf<T>();
            
            series["Leb128"].X.Add(maxSize);
            int compressedSizeLeb128 = compressed.Aggregate(0, (sizeBits, next) =>
            {
                EncodingUtils.EncodeLeb128((ulong)next, out int bits);
                return sizeBits + bits;
            }) / 8;
            series["Leb128"].Y.Add(1d - 1d * compressedSizeLeb128 / originalSize);
            
            series["Prefix"].X.Add(maxSize);
            int countBits = BitOperations.Log2((uint)BitOperations.Log2((uint)maxSize) + 1) + 1;
            int compressedSizePrefix = compressed.Aggregate(0, (sizeBits, next) =>
            {
                EncodingUtils.EncodePrefix((uint)next, countBits, out int bits);
                return sizeBits + bits;
            }) / 8;
            series["Prefix"].Y.Add(1d - 1d * compressedSizePrefix / originalSize);
            
            series["Huffman"].X.Add(maxSize);
            uint huffmanMaxBits = 32;
            var huffmanSymbols = PackageMerge.ComputeHuffmanTablePackageMerge(compressed, huffmanMaxBits);
            int huffmanEncodedSize = compressed.Aggregate(0, (sizeBits, next) => sizeBits + huffmanSymbols[next].length) / 8;
            int huffmanTableSize = huffmanSymbols.Aggregate(0, (sizeBits, next) => sizeBits + next.length) / 8;
            int compressedSizeHuffman = huffmanEncodedSize + huffmanTableSize;
            series["Huffman"].Y.Add(1d - 1d * compressedSizeHuffman / originalSize);
        }
        return series;
    }
}