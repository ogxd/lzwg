using System.Diagnostics;

namespace Lzwg;

internal class Decompressor<T>
{
    private readonly int _maxDictionarySize;
    private Dictionary<int, LinkedListNode<Entry>> _dictionary;
    private LinkedList<Entry> _lruOrder;
    private int _nextFreeIndex;

    public Decompressor(int maxDictionarySize)
    {
        _maxDictionarySize = maxDictionarySize;
    }

    public List<T> Decompress(List<int> compressedData, IReadOnlySet<T> dictionary)
    {
        _dictionary = new Dictionary<int, LinkedListNode<Entry>>();
        _lruOrder = new LinkedList<Entry>();
        _nextFreeIndex = 0;
        
        // Sort the dictionary to ensure consistent order
        foreach (var item in dictionary.Order())
        {
            ArraySegment<T> singleItem = new([item]);
            LinkedListNode<Entry> node = _lruOrder.AddLast(new Entry(_nextFreeIndex, singleItem));
            _dictionary[_nextFreeIndex] = node;
            _nextFreeIndex++;
        }
        
        List<T> output = new();
        ArraySegment<T>? previousSequence = null;
        
        foreach (int index in compressedData)
        {
            Trace.WriteLine($"Processing: {index}");
            
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
                
                Stack<LinkedListNode<Entry>> nodesToMove = new();
                var segment = node.Value.Segment;
                while (segment.Count > 0)
                {
                    LinkedListNode<Entry> currentNode = _dictionary.FirstOrDefault(x => x.Value.Value.Segment.SequenceEqual(segment)).Value;
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
                
                //MoveToMostRecentlyUsed(_lruOrder.Find());

                currentSequence = inferredSequence;

                AddToDictionary(inferredSequence);
            }

            output.AddRange(currentSequence);

            previousSequence = currentSequence;
        }

        return output;
    }
    
    private void AddToDictionary(ArraySegment<T> newSequence)
    {
        LinkedListNode<Entry> node = _lruOrder.AddFirst(new Entry(_nextFreeIndex, newSequence));
        _dictionary[_nextFreeIndex] = node;
        _nextFreeIndex++;
        
        Trace.WriteLine("- Added: " + string.Join("", newSequence) + " => " + (_nextFreeIndex - 1));
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
    
        // Reuse the index for future entries
        _nextFreeIndex = indexToRemove;
        
        Trace.WriteLine("- Removed: " + string.Join("", node.Value.Segment) + " => " + indexToRemove);
    }

    private void MoveToMostRecentlyUsed(LinkedListNode<Entry> node)
    {
        _lruOrder.Remove(node);
        _lruOrder.AddFirst(node);
        
        Trace.WriteLine($"- Moved: {string.Join("", node.Value.Segment)}");
    }
    
    private readonly record struct Entry(int Index, ArraySegment<T> Segment);
}