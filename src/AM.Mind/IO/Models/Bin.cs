using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Models;

// --- Binary helpers (varint, primitives, arrays) ---
internal static class Bin
{
    // Unsigned varint (LEB128-like; 7-bit chunks)
    public static void WriteVarUInt(Stream s, ulong value)
    {
        while (value >= 0x80)
        {
            s.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }
        s.WriteByte((byte)value);
    }

    public static (ulong value, int bytes) ReadVarUInt(Stream s)
    {
        ulong result = 0;
        int shift = 0, readBytes = 0;
        while (true)
        {
            int b = s.ReadByte();
            if (b < 0) throw new EndOfStreamException();
            readBytes++;
            result |= ((ulong)(b & 0x7F)) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
            if (shift > 63) throw new InvalidDataException("VarUInt too large");
        }
        return (result, readBytes);
    }
    // Non-throwing varint reader: returns false if we're exactly at EOF before reading any byte.
    // Throws if EOF occurs mid-varint (truncated record).
    public static bool TryReadVarUInt(Stream s, out ulong value)
    {
        value = 0;
        int shift = 0;
        int b = s.ReadByte();
        if (b < 0) return false; // clean EOF before any byte

        value |= (ulong)(b & 0x7F) << shift;
        if ((b & 0x80) == 0) return true;

        shift += 7;
        while (true)
        {
            b = s.ReadByte();
            if (b < 0) throw new EndOfStreamException("Truncated varint");

            value |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) return true;

            shift += 7;
            if (shift > 63) throw new InvalidDataException("VarUInt too large");
        }
    }
    public static void WriteInt32(Stream s, int v)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(buf, v);
        s.Write(buf);
    }
    public static int ReadInt32(Stream s)
    {
        Span<byte> buf = stackalloc byte[4];
        s.ReadExactly(buf);
        return BinaryPrimitives.ReadInt32LittleEndian(buf);
    }

    public static void WriteInt64(Stream s, long v)
    {
        Span<byte> buf = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(buf, v);
        s.Write(buf);
    }
    public static long ReadInt64(Stream s)
    {
        Span<byte> buf = stackalloc byte[8];
        s.ReadExactly(buf);
        return BinaryPrimitives.ReadInt64LittleEndian(buf);
    }

    public static void WriteFloat(Stream s, float v)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(buf, v);
        s.Write(buf);
    }
    public static float ReadFloat(Stream s)
    {
        Span<byte> buf = stackalloc byte[4];
        s.ReadExactly(buf);
        return BinaryPrimitives.ReadSingleLittleEndian(buf);
    }

    public static void WriteBool(Stream s, bool v) => s.WriteByte(v ? (byte)1 : (byte)0);
    public static bool ReadBool(Stream s)
    {
        int b = s.ReadByte();
        if (b < 0) throw new EndOfStreamException();
        return b != 0;
    }

    public static void WriteBytes(Stream s, byte[]? data)
    {
        if (data is null)
        {
            WriteVarUInt(s, 0);
            return;
        }
        WriteVarUInt(s, (ulong)data.Length);
        s.Write(data, 0, data.Length);
    }
    public static byte[]? ReadBytes(Stream s)
    {
        var (lenU, _) = ReadVarUInt(s);
        int len = checked((int)lenU);
        if (len == 0) return Array.Empty<byte>();
        var buf = new byte[len];
        s.ReadExactly(buf);
        return buf;
    }

    public static void WriteStringArray(Stream s, string[]? items)
    {
        if (items is null) { WriteVarUInt(s, 0); return; }
        WriteVarUInt(s, (ulong)items.Length);
        foreach (var it in items)
        {
            var bytes = Encoding.UTF8.GetBytes(it ?? string.Empty);
            WriteVarUInt(s, (ulong)bytes.Length);
            s.Write(bytes);
        }
    }
    public static string[]? ReadStringArray(Stream s)
    {
        var (nU, _) = ReadVarUInt(s);
        int n = checked((int)nU);
        if (n == 0) return Array.Empty<string>();
        var arr = new string[n];
        var buf = new byte[4096];
        for (int i = 0; i < n; i++)
        {
            var (lenU, _) = ReadVarUInt(s);
            int len = checked((int)lenU);
            if (buf.Length < len) buf = new byte[len];
            s.ReadExactly(buf.AsSpan(0, len));
            arr[i] = Encoding.UTF8.GetString(buf, 0, len);
        }
        return arr;
    }
}