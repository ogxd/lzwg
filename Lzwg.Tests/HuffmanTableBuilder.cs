using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Lzwg.Tests;

public static class PackageMerge 
{
    public static uint[] ComputeLengths(int[] frequencies, uint maxLen)
    {
        // Create sorted indices array
        var sorted = Enumerable.Range(0, frequencies.Length)
            .OrderBy(i => frequencies[i])
            .ToArray();

        int capacity = frequencies.Length * 2 - 1;
        var list = new List<int>(capacity);
        var flags = new uint[capacity];
        var merged = new List<int>(capacity);

        for (uint d = 0; d < maxLen; d++)
        {
            merged.Clear();
            uint mask = 1u << (int)d;

            // Create iterators for both merged pairs and sorted frequencies
            var pairIndex = 0;
            var sortedIndex = 0;
            
            while (pairIndex < list.Count / 2 * 2 || sortedIndex < sorted.Length)
            {
                bool takePair;
                
                if (pairIndex >= list.Count || pairIndex + 1 >= list.Count)
                    takePair = false;
                else if (sortedIndex >= sorted.Length)
                    takePair = true;
                else
                {
                    int pairSum = list[pairIndex] + list[pairIndex + 1];
                    takePair = pairSum.CompareTo(frequencies[sorted[sortedIndex]]) <= 0;
                }

                if (takePair)
                {
                    merged.Add(list[pairIndex] + list[pairIndex + 1]);
                    flags[merged.Count - 1] |= mask;
                    pairIndex += 2;
                }
                else
                {
                    merged.Add(frequencies[sorted[sortedIndex]]);
                    sortedIndex++;
                }
            }

            // Swap merged into list
            var temp = list;
            list = merged;
            merged = temp;
        }

        int n = frequencies.Length * 2 - 2;
        var codeLengths = new uint[frequencies.Length];
        uint depth = maxLen;

        while (depth > 0 && n > 0)
        {
            depth--;
            uint mask = 1u << (int)depth;
            int mergedCount = 0;

            for (int i = 0; i < n; i++)
            {
                if ((flags[i] & mask) == 0)
                {
                    codeLengths[sorted[i - mergedCount]]++;
                }
                else
                {
                    mergedCount++;
                }
            }

            n = mergedCount * 2;
        }

        return codeLengths;
    }
    
    public static HuffmanSymbol[] ComputeHuffmanTablePackageMerge(IReadOnlyList<int> values, uint maxLength)
    {
        int[] frequencies = new int[values.Max() + 1];
        foreach (var value in values)
        {
            //value.TrimBits(out int trimmedValue, out int significantBits);
            frequencies[value]++;
        }
        HuffmanCode[] codes = ComputeCodes(frequencies, maxLength);
        return codes.Select(c => new HuffmanSymbol { length = (int)c.Length, value = (int)c.Code }).ToArray();
    }
    
    public static HuffmanSymbol[] ComputeLimitedHuffmanTable(IReadOnlyList<int> values, int maxSymbols, uint maxLength, out HuffmanSymbol longTailSymbol)
    {
        int[] frequencies = new int[values.Max() + 1];
        foreach (var value in values)
        {
            frequencies[value]++;
        }
        
        Dictionary<int, int> map = new();
        var sorted = Enumerable.Range(0, frequencies.Length)
            .OrderBy(i => frequencies[i])
            .ToArray();

        int limitedLength = Math.Min(frequencies.Length, maxSymbols);
        var limitedFrequencies = new int[limitedLength];

        for (int i = 0; i < sorted.Length; i++)
        {
            if (i < maxSymbols - 1)
            {
                map[sorted[i]] = i;
                limitedFrequencies[i] = frequencies[sorted[i]];
            }
            else
            {
                map[sorted[i]] = maxSymbols - 1;
                limitedFrequencies[maxSymbols - 1] = 1; //frequencies[sorted[i]];
            }
        }
        
        HuffmanCode[] limitedCodes = ComputeCodes(limitedFrequencies, maxLength);

        HuffmanCode[] codes = new HuffmanCode[frequencies.Length];
        
        for (int i = 0; i < frequencies.Length; i++)
        {
            codes[i] = limitedCodes[map[i]];
        }

        if (frequencies.Length > maxSymbols)
        {
            longTailSymbol = new HuffmanSymbol { length = (int)limitedCodes[maxSymbols - 1].Length, value = (int)limitedCodes[maxSymbols - 1].Code };
        }
        else
        {
            longTailSymbol = new HuffmanSymbol { length = -1, value = -1 };
        }
        
        return codes.Select(c => new HuffmanSymbol { length = (int)c.Length, value = (int)c.Code }).ToArray();
    }
    
    public static HuffmanCode[] ComputeCodes(int[] frequencies, uint maxLen)
    {
        var lengths = ComputeLengths(frequencies, maxLen);
        return GenerateCanonicalCodes(lengths);
    }

    private static HuffmanCode[] GenerateCanonicalCodes(uint[] lengths)
    {
        var codes = new HuffmanCode[lengths.Length];
        
        // Initialize all codes
        for (int i = 0; i < codes.Length; i++)
        {
            codes[i] = new HuffmanCode { Length = lengths[i], Code = 0 };
        }

        // If all lengths are 0, return empty codes
        if (lengths.All(l => l == 0))
            return codes;

        // Find symbols for each bit length
        var blCount = new uint[lengths.Max() + 1];
        foreach (var length in lengths)
        {
            if (length > 0)
                blCount[length]++;
        }

        // Find the numerical value of the smallest code for each bit length
        var nextCode = new uint[blCount.Length];
        uint code = 0;
        blCount[0] = 0;
        for (uint bits = 1; bits < blCount.Length; bits++)
        {
            code = (code + blCount[bits - 1]) << 1;
            nextCode[bits] = code;
        }

        // Assign codes
        for (int n = 0; n < codes.Length; n++)
        {
            uint len = lengths[n];
            if (len != 0)
            {
                codes[n].Code = nextCode[len];
                nextCode[len]++;
            }
        }

        return codes;
    }
    
    public static Huffman16ReverseSymbol[] ExtendFull16BitTable(HuffmanSymbol[] table)
    {
        var extendedTable = new Huffman16ReverseSymbol[65536];
        for (int i = 0; i < table.Length; i++)
        {
            byte length = (byte)table[i].length;
            ushort value = (ushort)table[i].value;
            int start = value << (16 - length);
            int end = (value + 1) << (16 - length);
            for (int j = start; j < end; j++)
            {
                extendedTable[j] = new Huffman16ReverseSymbol { length = length, value = value, rle = (byte)i };
            }
        }
        return extendedTable;
    }

    public struct Huffman16ReverseSymbol
    {
        // The length of the code, so we know how many bits are not part of it (16 - length)
        // We need this information in order to read the next code at the correct position
        public byte length;

        // The value of the code. 16 bits at most
        public ushort value;

        public byte rle;
    }
    
    public struct HuffmanCode
    {
        public uint Length { get; set; }
        public uint Code { get; set; }

        public override string ToString()
        {
            // Convert code to binary string of length Length
            //return Convert.ToString(Code, 2).PadLeft((int)Length, '0').Substring((int)Math.Max(0, Convert.ToString(Code, 2).PadLeft((int)Length, '0').Length - Length));
            return Convert.ToString(Code, 2).PadLeft((int)Length, '0');//.Substring(32 - (int)Length);
        }
    }
}

public struct HuffmanSymbol
{
    // The huffman encoded symbol.
    public int value; // Todo: Use ushort
    // The numbers of bits of the symbol. 
    public int length; // Todo: Use byte
    
    public override string ToString()
    {
        // Only print the relevant bits
        if (length < 0)
        {
            return "invalid";
        }
        return Convert.ToString(value, 2).PadLeft(32,'0').Substring(32 - length);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is HuffmanSymbol symbol &&
               value == symbol.value &&
               length == symbol.length;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(value, length);
    }
}