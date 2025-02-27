namespace Lzwg;

public static class Lzwg
{
    public static List<int> Compress<T>(T[] input, IReadOnlySet<T> dictionary, int maxSize, int dictionaryResetSize = int.MaxValue)
    {
        Compressor<T> compressor = new(maxSize, dictionaryResetSize);
        return compressor.Compress(input, dictionary);
    }

    public static List<T> Decompress<T>(List<int> compressedData, IReadOnlySet<T> dictionary, int maxSize, int dictionaryResetSize = int.MaxValue)
    {
        Decompressor<T> decompressor = new(maxSize, dictionaryResetSize);
        return decompressor.Decompress(compressedData, dictionary);
    }
}