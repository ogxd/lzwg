using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lzwg;

internal class Compressor<T>
{
    private readonly int _maxDictionarySize;
    private readonly int _dictionaryResetSize;
    private Dictionary<ArraySegment<T>, (int nodeIndex, int index)> _dictionary;
    private OptimizedLinkedList<ArraySegment<T>> _lruOrder;
    private int _nextFreeIndex;

    public Compressor(int maxDictionarySize, int dictionaryResetSize = int.MaxValue)
    {
        _maxDictionarySize = maxDictionarySize;
        _dictionaryResetSize = dictionaryResetSize;
    }

    public List<int> Compress(T[] input, IReadOnlySet<T> dictionary)
    {
        if (dictionary.Count >= _maxDictionarySize)
        {
            throw new ArgumentException($"Base dictionary size ({dictionary.Count}) exceeds the maximum dictionary size");
        }
        
        Initialize(dictionary);

        List<int> output = new();
        int sequenceStart = 0;
        
        for (int i = 0; i < input.Length; i++)
        {
            Debug.WriteLine($"Processing: {input[i]}");
            
            var currentSequence = new ArraySegment<T>(input, sequenceStart, i - sequenceStart + 1);
            if (_dictionary.TryGetValue(currentSequence, out var value))
            {
                continue;
            }

            ArraySegment<T> previousSequence = new(input, sequenceStart, i - sequenceStart);
            if (_dictionary.TryGetValue(previousSequence, out value))
            {
                //MoveToMostRecentlyUsed(value.Item1);
                output.Add(value.Item2);
                //Debug.WriteLine($"- Output: {value.Item2}");
            }
            else
            {
                throw new InvalidOperationException("Should not happen");
            }
                
            for (int j = 1; j <= currentSequence.Count - 1; j++)
            {
                var segment = currentSequence[..j];
                if (!_dictionary.TryGetValue(segment, out var node))
                {
                    break;
                }
                MoveToMostRecentlyUsed(node.nodeIndex);
            }
                
            // Add new sequence to the dictionary
            AddToDictionary(currentSequence);
            sequenceStart = i;
            
            if (_dictionary.Count >= _dictionaryResetSize)
            {
                Debug.WriteLine("Dictionary reset");
                Initialize(dictionary);
            }
        }

        // Output the last sequence
        if (sequenceStart < input.Length)
        {
            ArraySegment<T> lastSequence = new(input, sequenceStart, input.Length - sequenceStart);
            if (_dictionary.TryGetValue(lastSequence, out (int nodeIndex, int index) value))
            {
                output.Add(value.index);
            }
            else
            {
                int index = AddToDictionary(lastSequence);
                output.Add(index);
            }
        }

        return output;
    }
    
    private void Initialize(IReadOnlySet<T> dictionary)
    {
        _dictionary = new Dictionary<ArraySegment<T>, (int nodeIndex, int index)>(new ArraySegmentEqualityComparer<T>());
        _lruOrder = new OptimizedLinkedList<ArraySegment<T>>();
        _nextFreeIndex = 0;
        
        // Sort the dictionary to ensure consistent order
        foreach (var item in dictionary.Order())
        {
            ArraySegment<T> singleItemList = new([item]);
            int nodeIndex = _lruOrder.AddLast(singleItemList);
            _dictionary[singleItemList] = (nodeIndex, GetNewIndex());
        }
    }

    private int AddToDictionary(ArraySegment<T> sequence)
    {
        if (_dictionary.Count >= _maxDictionarySize)
        {
            RemoveLeastRecentlyUsed();
        }

        var nodeIndex = _lruOrder.AddFirst(sequence);
        int index = GetNewIndex();
        _dictionary[sequence] = (nodeIndex, index);
        
        Debug.WriteLine("- Added: " + string.Join("", sequence) + " => " + index);
        
        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetNewIndex()
    {
        // It may either be the next free index or the dictionary size
        return _nextFreeIndex++;
    }

    /// <summary>
    /// Removes the least recently used multi-character entry from the dictionary.
    /// Single-character entries are preserved as they form the basic alphabet.
    /// </summary>
    private void RemoveLeastRecentlyUsed()
    {
        // Start from the least recently used entry (at the end of the list)
        int nodeIndex = _lruOrder.LastIndex;
        var node = _lruOrder[nodeIndex];
    
        // Skip all single-character entries as they must be preserved
        while (node.Value.Count == 1)
        {
            if (node.before < 0)
            {
                break;
            }
            nodeIndex = node.before;
            node = _lruOrder[node.before];
        }
        
        _lruOrder.Remove(nodeIndex);
        _dictionary.Remove(node.Value, out var removedValue);
        _nextFreeIndex = removedValue.index;
        
        Debug.WriteLine("- Removed: " + string.Join("", node.Value) + " => " + removedValue.index);
    }

    private void MoveToMostRecentlyUsed(int nodeIndex)
    {
        int newIndex = _lruOrder.MoveToFirst(nodeIndex);
        
        Debug.WriteLine($"- Moved: {string.Join("", _lruOrder[newIndex])}");
    }
}