namespace Lzwg;

public class LzwgCompressor<T>
{
    private readonly int _maxDictionarySize;
    private readonly Dictionary<ArraySegment<T>, (LinkedListNode<ArraySegment<T>> segment, int index)> _dictionary;
    private readonly LinkedList<ArraySegment<T>> _lruOrder;
    private int _nextFreeIndex;

    public LzwgCompressor(int maxDictionarySize, IList<T> dict)
    {
        _maxDictionarySize = maxDictionarySize;
        _dictionary = new Dictionary<ArraySegment<T>, (LinkedListNode<ArraySegment<T>> segment, int index)>(new ArraySegmentEqualityComparer<T>());
        _lruOrder = new LinkedList<ArraySegment<T>>();
        
        // Initialize the dictionary with single-character entries
        dict = dict.Distinct().ToList();
        foreach (var item in dict)
        {
            ArraySegment<T> singleItemList = new([item]);
            LinkedListNode<ArraySegment<T>> node = _lruOrder.AddLast(singleItemList);
            _dictionary[singleItemList] = (node, GetNewIndex());
        }
    }

    public List<int> Compress(T[] input)
    {
        List<int> output = new();
        int sequenceStart = 0;
        
        for (int i = 0; i < input.Length; i++)
        {
            //Console.WriteLine($"Processing: {input[i]}");
            
            var currentSequence = new ArraySegment<T>(input, sequenceStart, i - sequenceStart + 1);
            if (!_dictionary.TryGetValue(currentSequence, out var v))
            {
                ArraySegment<T> previousSequence = new(input, sequenceStart, i - sequenceStart);
                if (_dictionary.TryGetValue(previousSequence, out var value))
                {
                    MoveToMostRecentlyUsed(value.Item1);
                    output.Add(value.Item2);
                    
                    //Console.WriteLine($"- Output: {value.Item2}");
                }
                else
                {
                    throw new InvalidOperationException("Should not happen");
                }

                // Add new sequence to the dictionary
                AddToDictionary(currentSequence);
                sequenceStart = i;
            }
            
            //Console.WriteLine($"- LRU ({_lruOrder.Count}): {string.Join(", ", _lruOrder.Select(x => string.Join("", x) + " = " + _dictionary[x].index))}");
        }

        // Output the last sequence
        if (sequenceStart < input.Length)
        {
            ArraySegment<T> lastSequence = new(input, sequenceStart, input.Length - sequenceStart);
            if (_dictionary.TryGetValue(lastSequence, out (LinkedListNode<ArraySegment<T>> segment, int index) value))
            {
                // MoveToMostRecentlyUsed(previousSequence, value.Item1); // Not needed since it's the last sequence, but it could be kept for factorization
                output.Add(value.index);
                //Console.WriteLine($"- Output: {value.index}");
            }
            else
            {
                int index = AddToDictionary(lastSequence);
                output.Add(index);
                //Console.WriteLine($"- Output: {index}");
            }
            
            //Console.WriteLine($"- LRU ({_lruOrder.Count}): {string.Join(", ", _lruOrder.Select(x => string.Join("", x) + " = " + _dictionary[x].index))}");
        }

        return output;
    }

    private int AddToDictionary(ArraySegment<T> sequence)
    {
        if (_dictionary.Count >= _maxDictionarySize)
        {
            RemoveLeastRecentlyUsed(sequence[^1]);
        }

        LinkedListNode<ArraySegment<T>> node = _lruOrder.AddFirst(sequence);
        int index = GetNewIndex();
        _dictionary[sequence] = (node, index);
        
        Console.WriteLine("- Added: " + string.Join("", sequence) + " => " + index);
        
        return index;
    }

    private int GetNewIndex()
    {
        // It may either be the next free index or the dictionary size
        return _nextFreeIndex++;
    }

    private void RemoveLeastRecentlyUsed(T lastElement)
    {
        LinkedListNode<ArraySegment<T>> node = _lruOrder.Last!;
        for (;node.Value.Count == 1; node = node.Previous) {}
        
        _lruOrder.Remove(node);
        _dictionary.Remove(node.Value, out var removedValue);
        _nextFreeIndex = removedValue.index;
            
        Console.WriteLine("- Removed: " + string.Join("", node.Value) + " => " + removedValue.index);
    }

    private void MoveToMostRecentlyUsed(LinkedListNode<ArraySegment<T>> node)
    {
        Console.WriteLine($"- Moved: {string.Join("", node.Value)}");
        _lruOrder.Remove(node);
        _lruOrder.AddFirst(node);
    }
}