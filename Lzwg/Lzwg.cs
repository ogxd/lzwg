namespace Lzwg;

public static class Lzwg
{
    public static List<int> Compress<T>(T[] input, IReadOnlySet<T> dictionary, int maxSize)
    {
        Compressor<T> compressor = new(maxSize);
        return compressor.Compress(input, dictionary);
    }

    public static List<T> Decompress<T>(List<int> compressedData, IReadOnlySet<T> dictionary, int maxSize)
    {
        Decompressor<T> decompressor = new(maxSize);
        return decompressor.Decompress(compressedData, dictionary);
    }
}