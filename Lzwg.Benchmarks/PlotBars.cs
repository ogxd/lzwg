using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Lzwg.Tests;
using ScottPlot;

namespace Lzwg.Benchmarks;

public static class PlotBars
{
    public static void LoremIpsum()
    {
        // Load text from file
        var text = File.ReadAllText("LoremIpsum.txt").ToCharArray();
        ComputePlot("bars-lorem-ipsum", text);
    }
    
    public static void JpegRles()
    {
        var bytes = File.ReadAllBytes("jpegrles.bin");
        ComputePlot("bars-jpeg-rle", bytes);
    }
    
    public static void JpegDcts()
    {
        var bytes = File.ReadAllBytes("jpegdcts.bin");
        ComputePlot("bars-jpeg-dcts", bytes);
    }

    private static void ComputePlot<T>(string datasetName, T[] data)
    {
        var series = ComputeSeries(data);
        
        ScottPlot.Plot plot = new();
        List<Tick> ticks = new();
        foreach (var (name, serie) in series)
        {
            plot.Add.Bar(position: ticks.Count + 1, value: serie);
            ticks.Add(new (ticks.Count + 1, name));
        }
        plot.Title($"LZWG Compression for dataset '{datasetName}'");
        plot.YLabel("Compression ratio (higher is better)");

        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks.ToArray());
        plot.Axes.Bottom.MajorTickStyle.Length = 0;
        plot.HideGrid();
        
        plot.SavePng($"../../../{datasetName}.png", 800, 600);
    }

    // Y = Compression ratio
    // X = Max size
    private static Dictionary<string, double> ComputeSeries<T>(IReadOnlyList<T> originalData)
    {
        Dictionary<string, double> series = new();
    
        long timestamp = Stopwatch.GetTimestamp();
        int maxSize = 32000;
        List<int> compressed = Lzwg.Compress(originalData.ToArray(), originalData.ToHashSet(), maxSize);
        Console.WriteLine($"Compressed in {Stopwatch.GetElapsedTime(timestamp)}");
        
        int originalSize = originalData.Count * Marshal.SizeOf<T>();

        {
            int compressedSizeLeb128 = compressed.Aggregate(0, (totalSizeBits, next) =>
            {
                EncodingUtils.EncodeLeb128((ulong)next, out int bits);
                return totalSizeBits + bits;
            }) / 8;
            series["Leb128"] = 1d - 1d * compressedSizeLeb128 / originalSize;
        }

        {
            int dictSize = 0;
            int compressedSizePrefix = compressed.Aggregate(0, (totalSizeBits, next) =>
            {
                dictSize = Math.Min(dictSize + 1, maxSize);
                int sizePrefixSizeBits = BitOperations.Log2((uint)BitOperations.Log2((uint)dictSize) + 1) + 1;
                EncodingUtils.EncodePrefix((uint)next, sizePrefixSizeBits, out int bits);
                return totalSizeBits + bits;
            }) / 8;
            series["Prefix"]= 1d - 1d * compressedSizePrefix / originalSize;
        }
        
        {
            int dictSize = 0;
            int compressedSizePrefix = compressed.Aggregate(0, (totalSizeBits, _) =>
            {
                dictSize = Math.Min(dictSize + 1, maxSize);
                int sizeBits = BitOperations.Log2((uint)dictSize) + 1;
                return totalSizeBits + sizeBits;
            }) / 8;
            series["Fixed"] = 1d - 1d * compressedSizePrefix / originalSize;
        }

        {
            uint huffmanMaxBits = 32;
            var huffmanSymbols = PackageMerge.ComputeHuffmanTablePackageMerge(compressed, huffmanMaxBits);
            int huffmanEncodedSize = compressed.Aggregate(0, (totalSizeBits, next) => totalSizeBits + huffmanSymbols[next].length) / 8;
            int huffmanTableSize = huffmanSymbols.Aggregate(0, (totalSizeBits, next) => totalSizeBits + next.length) / 8;
            int compressedSizeHuffman = huffmanEncodedSize + huffmanTableSize;
            series["Huffman"] = 1d - 1d * compressedSizeHuffman / originalSize;
        }
        
        return series;
    }
}