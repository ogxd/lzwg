namespace Lzwg;

internal class ArraySegmentEqualityComparer<T> : IEqualityComparer<ArraySegment<T>>
{
    public bool Equals(ArraySegment<T> x, ArraySegment<T> y)
    {
        if (x.Count != y.Count)
            return false; // Different lengths

        return x.SequenceEqual(y); // Compare elements
    }

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
