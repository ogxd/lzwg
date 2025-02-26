# LZWG

LZWG (Lempel–Ziv–Welch-Giniaux) is a variant of the LZW algorithm that keeps tracks of the least recently used entries in the dictionary to evict them and effectively bound the dictionary size. This addresses the main drawback of the LZW algorithm where the dictionary size can grow indefinitely, producting symbols of high cardinality, which becomes a limiting factor when it comes to encoding them in as little bits as possible.  
Using LZWG with an `int.MaxValue` as the maximum dictionary size will effectively make it behave like the LZW algorithm.

## Usage

```csharp
using Lzwg;

string loremIpsum = ...;
List<int> compressed = Lzwg.Compress<char>(loremIpsum, BaseDictionaries.Ascii, 1000);
List<char> decompressed = Lzwg.Decompress<char>(compressed, BaseDictionaries.Ascii, 1000);

Assert.AreEqual(loremIpsum, new string(CollectionsMarshal.AsSpan(decompressed)));
```

Notes:
- You must use the same dictionary and max size for both compression and decompression.
- Max size must be greater than the dictionary size (or an exception will be thrown).
- As for any LZW the methods are not thread-safe.

## Performance

This C# implementation uses `ArraySegment<T>` for minimal allocations and O(1) operations all the way through. This makes the implementation extremely efficient (both compression and decompression).  

## Todo

Todo before release:

- [x] Refactor API
- [ ] Cleanup code
- [ ] Use optimized linked list
- [ ] Improve test coverage
- [ ] Benchmark
- [ ] Improve readme
