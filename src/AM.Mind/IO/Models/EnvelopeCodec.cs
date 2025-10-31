using AM.Mind.IO.Enums;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Models;

// --- Envelope (de)serialization (binary, versioned) ---
internal static class EnvelopeCodec
{
    // Payload layout (little-endian):
    // [Version u16]=1
    // [Id i64][Ticks i64][Episode i32][Step i32][Reward f32][Terminal u8][ObsKind u8][ActKind u8][Pad u8]
    // [ObsPayload bytes(varlen)]
    // [ActPayload bytes(varlen)]
    // [HasNext u8][NextObs bytes(varlen if HasNext=1)]
    // [Tags string[] (varlen count + per-item varlen)]
    public static byte[] Serialize(ExperienceEnvelope e)
    {
        using var ms = new MemoryStream(256 + (e.ObsPayload?.Length ?? 0) + (e.ActPayload?.Length ?? 0) + (e.NextObsPayload?.Length ?? 0));
        Span<byte> head = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(head, 1);
        ms.Write(head);

        Bin.WriteInt64(ms, e.Id);
        Bin.WriteInt64(ms, e.TicksUtc);
        Bin.WriteInt32(ms, e.Episode);
        Bin.WriteInt32(ms, e.Step);
        Bin.WriteFloat(ms, e.Reward);
        Bin.WriteBool(ms, e.Terminal);

        ms.WriteByte((byte)e.ObsType);
        ms.WriteByte((byte)e.ActType);
        ms.WriteByte(0); // padding/reserved

        Bin.WriteBytes(ms, e.ObsPayload);
        Bin.WriteBytes(ms, e.ActPayload);

        ms.WriteByte(e.NextObsPayload is null ? (byte)0 : (byte)1);
        if (e.NextObsPayload is not null)
            Bin.WriteBytes(ms, e.NextObsPayload);

        Bin.WriteStringArray(ms, e.Tags);
        return ms.ToArray();
    }

    public static ExperienceEnvelope Deserialize(ReadOnlySpan<byte> payload)
    {
        using var ms = new MemoryStream(payload.ToArray(), writable: false);
        Span<byte> ver = stackalloc byte[2];
        ms.ReadExactly(ver);
        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(ver);
        if (version != 1) throw new InvalidDataException($"Unknown envelope version {version}");

        var e = new ExperienceEnvelope
        {
            Id = Bin.ReadInt64(ms),
            TicksUtc = Bin.ReadInt64(ms),
            Episode = Bin.ReadInt32(ms),
            Step = Bin.ReadInt32(ms),
            Reward = Bin.ReadFloat(ms),
            Terminal = Bin.ReadBool(ms),
            ObsType = (ObsKind)ms.ReadByte(),
            ActType = (ActKind)ms.ReadByte()
        };
        ms.ReadByte(); // pad

        e.ObsPayload = Bin.ReadBytes(ms) ?? Array.Empty<byte>();
        e.ActPayload = Bin.ReadBytes(ms) ?? Array.Empty<byte>();

        bool hasNext = Bin.ReadBool(ms);
        if (hasNext) e.NextObsPayload = Bin.ReadBytes(ms);

        e.Tags = Bin.ReadStringArray(ms);
        return e;
    }
}