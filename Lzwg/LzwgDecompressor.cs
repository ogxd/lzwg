namespace Lzwg;

public class LzwgDecompressor<T>
{
    private readonly int _maxDictionarySize;
    private readonly Dictionary<int, (LinkedListNode<ArraySegment<T>> segment, ArraySegment<T> sequence)> _dictionary;
    private readonly LinkedList<ArraySegment<T>> _lruOrder;
    private int _nextFreeIndex;

    public LzwgDecompressor(int maxDictionarySize, IList<T> dict)
    {
        _maxDictionarySize = maxDictionarySize;
        _dictionary = new Dictionary<int, (LinkedListNode<ArraySegment<T>>, ArraySegment<T>)>();
        _lruOrder = new LinkedList<ArraySegment<T>>();
        _nextFreeIndex = 0;

        // Initialize the dictionary with single-character entries
        dict = dict.Distinct().ToList();
        foreach (var item in dict)
        {
            ArraySegment<T> singleItem = new([item]);
            LinkedListNode<ArraySegment<T>> node = _lruOrder.AddLast(singleItem);
            _dictionary[_nextFreeIndex++] = (node, singleItem);
        }
    }

    public List<T> Decompress(List<int> compressedData)
    {
        List<T> output = new();
        ArraySegment<T>? previousSequence = null;
        
        foreach (int index in compressedData)
        {
            Console.WriteLine($"Processing: {index}");
            
            ArraySegment<T> currentSequence;

            if (_dictionary.TryGetValue(index, out var value))
            {
                currentSequence = value.sequence;
                bool move = true;
                if (previousSequence != null)
                {
                    Console.WriteLine($"- Previous: {string.Join("", previousSequence.Value)}");
                    Console.WriteLine($"- Current: {string.Join("", currentSequence)}");
                    // Add new sequence to the dictionary
                
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
                
                if (move)
                    MoveToMostRecentlyUsed(value.segment); // Hack to match compression: current sequence must be placed as MRU
            }
            else
            {
                // Special case: The new sequence is previousSequence + first element of previousSequence
                var inferredSequence = previousSequence.Value.Concat([previousSequence.Value[0]]).ToArray();
    
                //Console.WriteLine($"- Special Case: Adding inferred sequence {string.Join("", inferredSequence)}");

                currentSequence = inferredSequence;

                AddToDictionary(inferredSequence);
            }

            output.AddRange(currentSequence);
            
            //Console.WriteLine($"- Output: {string.Join("", currentSequence)}");

            previousSequence = currentSequence;
            
            Console.WriteLine($"- LRU ({_lruOrder.Count}): {string.Join(", ", _lruOrder.Select(x => string.Join("", x) + " = " + _dictionary.FirstOrDefault(y => y.Value.sequence.Equals(x)).Key))}");
        }

        return output;
    }
    
    private void AddToDictionary(ArraySegment<T> newSequence)
    {
        LinkedListNode<ArraySegment<T>> node = _lruOrder.AddFirst(newSequence);
        _dictionary[_nextFreeIndex++] = (node, newSequence);
        
        Console.WriteLine("- Added: " + string.Join("", newSequence) + " => " + (_nextFreeIndex - 1));
    }

    private void RemoveLeastRecentlyUsed()
    {
        if (_lruOrder.Count == 0)
            return;

        LinkedListNode<ArraySegment<T>> node = _lruOrder.Last!;
        for (;node.Value.Count == 1; node = node.Previous) {}
        
        _lruOrder.Remove(node);
        int index = _dictionary.FirstOrDefault(x => x.Value.segment == node).Key;
        _dictionary.Remove(index, out var removedValue);
        _nextFreeIndex = index;
            
        Console.WriteLine("- Removed: " + string.Join("", node.Value) + " => " + index);
    }

    private void MoveToMostRecentlyUsed(LinkedListNode<ArraySegment<T>> node)
    {
        Console.WriteLine($"- Moved: {string.Join("", node.Value)}");
        _lruOrder.Remove(node);
        _lruOrder.AddFirst(node);
    }
}