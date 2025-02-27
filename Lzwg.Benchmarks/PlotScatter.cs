using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Lzwg.Tests;

namespace Lzwg.Benchmarks;

public class Serie
{
    public List<double> X = new();
    public List<double> Y = new();
}

public static class PlotScatter
{
    public static void LoremIpsum()
    {
        // Load text from file
        var text = File.ReadAllText("LoremIpsum.txt").ToCharArray();
        ComputePlot("lorem-ipsum", text, 8, 16);
    }
    
    public static void JpegRles()
    {
        var bytes = File.ReadAllBytes("jpegrles.bin");
        ComputePlot("jpeg-rle", bytes);
    }
    
    public static void JpegDcts()
    {
        var bytes = File.ReadAllBytes("jpegdcts.bin");
        ComputePlot("jpeg-dcts", bytes);
    }

    private static void ComputePlot<T>(string datasetName, T[] data, int minLog2 = 9, int maxLog2 = 24)
    {
        var series = ComputeSeries(data, minLog2, maxLog2);
        
        ScottPlot.Plot plot = new();
        foreach (var (name, serie) in series)
        {
            var scatter = plot.Add.Scatter(serie.X.Select(Math.Log2).ToArray(), serie.Y.ToArray());
            scatter.LegendText = name;
        }
        plot.ShowLegend();
        plot.Legend.Alignment = ScottPlot.Alignment.LowerLeft;
        plot.Title($"LZWG Compression for dataset '{datasetName}'");
        plot.XLabel("Maximum dictionary size");
        plot.YLabel("Compression ratio (higher is better)");
        ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGen = new();
        ScottPlot.TickGenerators.NumericAutomatic tickGen = new();
        tickGen.MinorTickGenerator = minorTickGen;
        static string LogTickLabelFormatter(double y) => $"{Math.Pow(2, y):N0}";
        tickGen.IntegerTicksOnly = true;
        tickGen.LabelFormatter = LogTickLabelFormatter;
        plot.Axes.Bottom.TickGenerator = tickGen;
        plot.SavePng($"../../../{datasetName}.png", 800, 600);
    }

    // Y = Compression ratio
    // X = Max size
    private static Dictionary<string, Serie> ComputeSeries<T>(IReadOnlyList<T> originalData, int minLog2, int maxLog2)
    {
        Dictionary<string, Serie> series = new();
        series["Leb128"] = new Serie();
        series["Prefix"] = new Serie();
        series["Fixed"] = new Serie();
        series["Huffman"] = new Serie();
        
        for (int i = minLog2; i <= maxLog2; i++)
        {
            int maxSize = (int)Math.Pow(2, i);
            
            Console.WriteLine($"Processing... maxSize={maxSize}");

            long timestamp = Stopwatch.GetTimestamp();
            List<int> compressed = Lzwg.Compress(originalData.ToArray(), originalData.ToHashSet(), maxSize);
            Console.WriteLine($"Compressed in {Stopwatch.GetElapsedTime(timestamp)}");
            
            int originalSize = originalData.Count * Marshal.SizeOf<T>();

            {
                series["Leb128"].X.Add(maxSize);
                int compressedSizeLeb128 = compressed.Aggregate(0, (totalSizeBits, next) =>
                {
                    EncodingUtils.EncodeLeb128((ulong)next, out int bits);
                    return totalSizeBits + bits;
                }) / 8;
                series["Leb128"].Y.Add(1d - 1d * compressedSizeLeb128 / originalSize);
            }

            {
                series["Prefix"].X.Add(maxSize);
                int dictSize = 0;
                int compressedSizePrefix = compressed.Aggregate(0, (totalSizeBits, next) =>
                {
                    dictSize = Math.Min(dictSize + 1, maxSize);
                    int sizePrefixSizeBits = BitOperations.Log2((uint)BitOperations.Log2((uint)dictSize) + 1) + 1;
                    EncodingUtils.EncodePrefix((uint)next, sizePrefixSizeBits, out int bits);
                    return totalSizeBits + bits;
                }) / 8;
                series["Prefix"].Y.Add(1d - 1d * compressedSizePrefix / originalSize);
            }
            
            {
                series["Fixed"].X.Add(maxSize);
                int dictSize = 0;
                int compressedSizePrefix = compressed.Aggregate(0, (totalSizeBits, _) =>
                {
                    dictSize = Math.Min(dictSize + 1, maxSize);
                    int sizeBits = BitOperations.Log2((uint)dictSize) + 1;
                    return totalSizeBits + sizeBits;
                }) / 8;
                series["Fixed"].Y.Add(1d - 1d * compressedSizePrefix / originalSize);
            }

            {
                series["Huffman"].X.Add(maxSize);
                uint huffmanMaxBits = 32;
                var huffmanSymbols = PackageMerge.ComputeHuffmanTablePackageMerge(compressed, huffmanMaxBits);
                int huffmanEncodedSize = compressed.Aggregate(0, (totalSizeBits, next) => totalSizeBits + huffmanSymbols[next].length) / 8;
                int huffmanTableSize = huffmanSymbols.Aggregate(0, (totalSizeBits, next) => totalSizeBits + next.length) / 8;
                int compressedSizeHuffman = huffmanEncodedSize + huffmanTableSize;
                series["Huffman"].Y.Add(1d - 1d * compressedSizeHuffman / originalSize);
            }
        }
        return series;
    }
}