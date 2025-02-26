namespace Lzwg;

internal class ArraySegmentEqualityComparer<T> : IEqualityComparer<ArraySegment<T>>
{
    public bool Equals(ArraySegment<T> x, ArraySegment<T> y)
    {
        if (x.Count != y.Count)
            return false; // Different lengths

        return x.SequenceEqual(y); // Compare elements
    }

    // We could optimize by computing the hash only once and storing it in a field, but it requires a custom structure
    public int GetHashCode(ArraySegment<T> obj)
    {
        HashCode hash = new();
        foreach (var item in obj)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}
