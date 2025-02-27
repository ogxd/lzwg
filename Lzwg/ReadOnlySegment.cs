using System.Collections;
using System.Runtime.CompilerServices;

namespace Lzwg;

internal readonly struct ReadOnlySegment<T> : IEquatable<ReadOnlySegment<T>>, IEnumerable<T>
{
    private readonly T[] _array;
    private readonly int _offset;
    private readonly int _count;
    private readonly int _hashCode;
    
    public int Count => _count;
    
    public ReadOnlySegment(T[] array, int offset, int count)
    {
        _array = array;
        _offset = offset;
        _count = count;
        _hashCode = ComputeHashCode();
    }

    public bool Equals(ReadOnlySegment<T> other)
    {
        if (_count != other._count)
        {
            return false;
        }
        if (_array == other._array && _offset == other._offset)
        {
            return true;
        }
        return this.SequenceEqual(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => _hashCode;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ComputeHashCode()
    {
        HashCode hash = new();
        for (int i = 0; i < _count; i++)
        {
            hash.Add(_array[_offset + i]);
        }
        return hash.ToHashCode();
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            yield return _array[_offset + i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySegment<T> Slice(int start, int length) {
        return new ReadOnlySegment<T>(_array, _offset + start, length);
    }
}