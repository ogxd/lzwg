using System.Diagnostics;

namespace Lzwg;

internal class Decompressor<T>
{
    private readonly int _maxDictionarySize;
    private readonly int _dictionaryResetSize;
    private Dictionary<int, LinkedListNode<Entry>> _dictionary;
    private Dictionary<ArraySegment<T>, LinkedListNode<Entry>> _inverseDictionary;
    private LinkedList<Entry> _lruOrder;
    private int _nextFreeIndex;

    public Decompressor(int maxDictionarySize, int dictionaryResetSize = int.MaxValue)
    {
        _maxDictionarySize = maxDictionarySize;
        _dictionaryResetSize = dictionaryResetSize;
    }

    public List<T> Decompress(List<int> compressedData, IReadOnlySet<T> dictionary)
    {
        Initialize(dictionary);

        List<T> output = new();
        ArraySegment<T>? previousSequence = null;
        
        foreach (int index in compressedData)
        {
            Debug.WriteLine($"Processing: {index}");
            
            ArraySegment<T> currentSequence;

            if (_dictionary.TryGetValue(index, out var node))
            {
                currentSequence = node.Value.Segment;
                bool move = true;
                if (previousSequence != null)
                {
                    if (_dictionary.Count >= _maxDictionarySize)
                    {
                        RemoveLeastRecentlyUsed();
                    }

                    var sequence = previousSequence.Value.Concat([currentSequence[0]]).ToArray();
                    if (!_dictionary.TryGetValue(index, out _))
                    {
                        sequence = previousSequence.Value.Concat([previousSequence.Value[0]]).ToArray();
                        move = false;
                        currentSequence = sequence;
                    }
                    
                    AddToDictionary(sequence);
                }
                
                for (int i = 1; i <= node.Value.Segment.Count; i++)
                {
                    var segment = node.Value.Segment[..i];
                    LinkedListNode<Entry> currentNode = _inverseDictionary.TryGetValue(segment, out var x) ? x : null;
                    if (currentNode == null)
                    {
                        break;
                    }
                    if (move)
                    {
                        MoveToMostRecentlyUsed(currentNode);
                    }
                }
            }
            else
            {
                // Special case: The new sequence is previousSequence + first element of previousSequence
                T[] inferredSequence = previousSequence.Value.Concat([previousSequence.Value[0]]).ToArray();
                
                currentSequence = inferredSequence;

                AddToDictionary(inferredSequence);
            }

            output.AddRange(currentSequence);

            previousSequence = currentSequence;

            if (_dictionary.Count >= _dictionaryResetSize)
            {
                Debug.WriteLine("Dictionary reset");
                Initialize(dictionary);
            }
        }

        return output;
    }

    private void Initialize(IReadOnlySet<T> dictionary)
    {
        _dictionary = new Dictionary<int, LinkedListNode<Entry>>();
        _inverseDictionary = new Dictionary<ArraySegment<T>, LinkedListNode<Entry>>(new ArraySegmentEqualityComparer<T>());
        _lruOrder = new LinkedList<Entry>();
        _nextFreeIndex = 0;
        
        // Sort the dictionary to ensure consistent order
        foreach (var item in dictionary.Order())
        {
            ArraySegment<T> singleItem = new([item]);
            LinkedListNode<Entry> node = _lruOrder.AddLast(new Entry(_nextFreeIndex, singleItem));
            _dictionary[_nextFreeIndex] = node;
            _inverseDictionary[singleItem] = node;
            _nextFreeIndex++;
        }
    }

    private void AddToDictionary(ArraySegment<T> newSequence)
    {
        LinkedListNode<Entry> node = _lruOrder.AddFirst(new Entry(_nextFreeIndex, newSequence));
        _dictionary[_nextFreeIndex] = node;
        _inverseDictionary[newSequence] = node;
        _nextFreeIndex++;
        
        Debug.WriteLine("- Added: " + string.Join("", newSequence) + " => " + (_nextFreeIndex - 1));
    }

    /// <summary>
    /// Removes the least recently used multi-character entry from the dictionary.
    /// Single-character entries are preserved as they form the basic alphabet.
    /// </summary>
    private void RemoveLeastRecentlyUsed()
    {
        // Start from the least recently used entry (at the end of the list)
        LinkedListNode<Entry> node = _lruOrder.Last!;
    
        // Skip all single-character entries as they must be preserved
        while (node != null && node.Value.Segment.Count == 1)
        {
            node = node.Previous!;
        }
        
        int indexToRemove = node!.Value.Index;
    
        // Remove from both the LRU order list and the dictionary
        _lruOrder.Remove(node);
        _dictionary.Remove(indexToRemove, out _);
        _inverseDictionary.Remove(node.Value.Segment);
    
        // Reuse the index for future entries
        _nextFreeIndex = indexToRemove;
        
        Debug.WriteLine("- Removed: " + string.Join("", node.Value.Segment) + " => " + indexToRemove);
    }

    private void MoveToMostRecentlyUsed(LinkedListNode<Entry> node)
    {
        _lruOrder.Remove(node);
        _lruOrder.AddFirst(node);
        
        Debug.WriteLine($"- Moved: {string.Join("", node.Value.Segment)}");
    }
    
    private readonly record struct Entry(int Index, ArraySegment<T> Segment);
}