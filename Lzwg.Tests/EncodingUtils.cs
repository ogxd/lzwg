using System.Numerics;

namespace Lzwg.Tests;

public static class EncodingUtils
{
    public static void EncodeLeb128(ulong value, out int bits) {
        bits = 0;
        bool more = true;

        while (more) {
            byte chunk = (byte)(value & 0x7fUL); // extract a 7-bit chunk
            value >>= 7;

            more = value != 0;
            if (more) { chunk |= 0x80; } // set msb marker that more bytes are coming

            //stream.WriteByte(chunk);
            bits += 8;
        };
    }

    public static void EncodePrefix(uint value, int sizeHintBits, out int bits)
    {
        // Compute leading zeroes (value from 0 to 32)
        int leadingZeroes = BitOperations.LeadingZeroCount(value);
        int valueSize = 32 - leadingZeroes;
        
        // Write value size (requires at most 5 bits, because 2^5 = 32, which is the maximum value size)
        //WriteLowBits(valueSize, sizeHintBits);
        bits = sizeHintBits;
        
        // Write value
        //WriteLowBits((int)value, valueSize);
        bits += valueSize;
    }
}