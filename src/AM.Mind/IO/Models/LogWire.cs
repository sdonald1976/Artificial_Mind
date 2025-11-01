using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Models;

internal static class LogWire
{
    public const uint MAGIC = 0x584C4F47; // 'XLOG'
    public const ushort VERSION = 1;

    public static void WriteHeader(Stream s)
    {
        Span<byte> buf = stackalloc byte[6];
        BinaryPrimitives.WriteUInt32LittleEndian(buf[..4], MAGIC);
        BinaryPrimitives.WriteUInt16LittleEndian(buf[4..], VERSION);
        s.Write(buf);
    }

    public static void ReadAndValidateHeader(Stream s)
    {
        Span<byte> buf = stackalloc byte[6];
        int n = s.Read(buf);
        if (n != 6) throw new IOException("Truncated xlog header.");
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(buf[..4]);
        ushort ver = BinaryPrimitives.ReadUInt16LittleEndian(buf[4..]);
        if (magic != MAGIC) throw new AM.Mind.Errors.ExperienceFormatException("Bad XLOG magic.");
        if (ver != VERSION) throw new AM.Mind.Errors.VersionMismatchException($"XLOG version {ver} != {VERSION}");
    }

    // Portable checksum (FNV-1a 32-bit)
    public static uint ComputeChecksum(ReadOnlySpan<byte> payload)
    {
        const uint fnvOffset = 2166136261;
        const uint fnvPrime = 16777619;
        uint hash = fnvOffset;
        for (int i = 0; i < payload.Length; i++)
        {
            hash ^= payload[i];
            hash *= fnvPrime;
        }
        return hash;
    }
}