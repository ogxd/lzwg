using System.Diagnostics;

namespace Lzwg;

internal class Compressor<T>
{
    private readonly int _maxDictionarySize;
    private Dictionary<ArraySegment<T>, (LinkedListNode<ArraySegment<T>> node, int index)> _dictionary;
    private LinkedList<ArraySegment<T>> _lruOrder;
    private int _nextFreeIndex;

    public Compressor(int maxDictionarySize)
    {
        _maxDictionarySize = maxDictionarySize;
    }

    public List<int> Compress(T[] input, IReadOnlySet<T> dictionary)
    {
        if (dictionary.Count >= _maxDictionarySize)
        {
            throw new ArgumentException($"Base dictionary size ({dictionary.Count}) exceeds the maximum dictionary size");
        }
        
        _dictionary = new Dictionary<ArraySegment<T>, (LinkedListNode<ArraySegment<T>> node, int index)>(new ArraySegmentEqualityComparer<T>());
        _lruOrder = new LinkedList<ArraySegment<T>>();
        _nextFreeIndex = 0;
        
        // Sort the dictionary to ensure consistent order
        foreach (var item in dictionary.Order())
        {
            ArraySegment<T> singleItemList = new([item]);
            LinkedListNode<ArraySegment<T>> node = _lruOrder.AddLast(singleItemList);
            _dictionary[singleItemList] = (node, GetNewIndex());
        }
        
        List<int> output = new();
        int sequenceStart = 0;
        
        for (int i = 0; i < input.Length; i++)
        {
            Debug.WriteLine($"Processing: {input[i]}");
            
            var currentSequence = new ArraySegment<T>(input, sequenceStart, i - sequenceStart + 1);
            if (_dictionary.TryGetValue(currentSequence, out var value))
            {
                //MoveToMostRecentlyUsed2(value.node);
            }
            else
            {
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
                
                Stack<LinkedListNode<ArraySegment<T>>> nodesToMove = new();
                var segment = currentSequence[..^1];
                while (segment.Count > 0)
                {
                    LinkedListNode<ArraySegment<T>> currentNode = _dictionary.TryGetValue(segment, out var node) ? node.node : null;
                    nodesToMove.Push(currentNode);
                    //MoveToMostRecentlyUsed(currentNode);
                    //break;
                    segment = segment[..^1];
                }
                while (nodesToMove.Count > 0)
                {
                    var currentNode = nodesToMove.Pop();
                    if (currentNode == null)
                    {
                        break;
                    }
                    MoveToMostRecentlyUsed(currentNode);
                }
                
                // Add new sequence to the dictionary
                AddToDictionary(currentSequence);
                sequenceStart = i;
                
                // if (_dictionary.TryGetValue(new ArraySegment<T>(input, sequenceStart, 1), out value))
                // {
                //     MoveToMostRecentlyUsed(value.Item1);
                // }
            }
        }

        // Output the last sequence
        if (sequenceStart < input.Length)
        {
            ArraySegment<T> lastSequence = new(input, sequenceStart, input.Length - sequenceStart);
            if (_dictionary.TryGetValue(lastSequence, out (LinkedListNode<ArraySegment<T>> segment, int index) value))
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

    private int AddToDictionary(ArraySegment<T> sequence)
    {
        if (_dictionary.Count >= _maxDictionarySize)
        {
            RemoveLeastRecentlyUsed();
        }

        LinkedListNode<ArraySegment<T>> node = _lruOrder.AddFirst(sequence);
        int index = GetNewIndex();
        _dictionary[sequence] = (node, index);
        
        Debug.WriteLine("- Added: " + string.Join("", sequence) + " => " + index);
        
        return index;
    }

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
        LinkedListNode<ArraySegment<T>> node = _lruOrder.Last!;
    
        // Skip all single-character entries as they must be preserved
        while (node != null && node.Value.Count == 1)
        {
            node = node.Previous!;
        }
        
        _lruOrder.Remove(node!);
        _dictionary.Remove(node!.Value, out var removedValue);
        _nextFreeIndex = removedValue.index;
        
        Debug.WriteLine("- Removed: " + string.Join("", node.Value) + " => " + removedValue.index);
    }

    private void MoveToMostRecentlyUsed(LinkedListNode<ArraySegment<T>> node)
    {
        _lruOrder.Remove(node);
        _lruOrder.AddFirst(node);
        
        Debug.WriteLine($"- Moved: {string.Join("", node.Value)}");
    }
    private void MoveToMostRecentlyUsed2(LinkedListNode<ArraySegment<T>> node)
    {
        _lruOrder.Remove(node);
        _lruOrder.AddFirst(node);
        
        Debug.WriteLine($"- Moved: {string.Join("", node.Value)} *");
    }
}