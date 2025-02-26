namespace Lzwg;

public static class BaseDictionaries
{
    static BaseDictionaries()
    {
        Ascii = Enumerable.Range(0, 128).Select(i => (char)i).ToHashSet();
        Bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToHashSet();
    }

    public static IReadOnlySet<char> Ascii;
    public static IReadOnlySet<byte> Bytes;
}