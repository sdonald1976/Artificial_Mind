using AM.Mind.IO.Enums;
using AM.Mind.IO.Interfaces;
using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Models;

public sealed class Lz4Codec : ICodec
{
    public XLogFlags Flags => (XLogFlags)1; // define LZ4 = 1
    public byte[] Encode(ReadOnlySpan<byte> src)
    {
        int maxLen = LZ4Codec.MaximumOutputSize(src.Length);
        var dst = new byte[maxLen];
        int written = LZ4Codec.Encode(src, dst, LZ4Level.L00_FAST);
        Array.Resize(ref dst, written);
        return dst;
    }

    public byte[] Decode(ReadOnlySpan<byte> src)
    {
        // You need the uncompressed length to decode. Easiest: store it before the payload.
        // The writer can write [origLen varint][compressed bytes]
        throw new NotImplementedException();
    }
}