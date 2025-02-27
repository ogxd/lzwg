namespace Lzwg;

internal class ArraySegmentEqualityComparer<T> : IEqualityComparer<ArraySegment<T>>
{
    public bool Equals(ArraySegment<T> x, ArraySegment<T> y)
    {
        // Turbo path
        if (x.Count != y.Count)
            return false;
        
        // Fast path to skip the sequence comparison if the references are the same
        if (x.Array == y.Array && x.Offset == y.Offset)
            return true;

        // Slow path, compare elements one by one
        for (int i = 0; i < x.Count; i++)
        {
            if (!x[i]!.Equals(y[i]))
            {
                return false;
            }
        }
        
        return true;
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
