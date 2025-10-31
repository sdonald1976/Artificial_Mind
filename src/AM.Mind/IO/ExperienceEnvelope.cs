using AM.Mind.IO.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO;

// NOTE: Kept as a plain class with simple binary serializer below (no MessagePack needed).
public sealed class ExperienceEnvelope
{
    public long Id;            // global, monotonically increasing
    public long TicksUtc;      // DateTime.UtcNow.Ticks
    public int Episode;
    public int Step;
    public float Reward;
    public bool Terminal;
    public ObsKind ObsType;
    public ActKind ActType;

    public byte[] ObsPayload = Array.Empty<byte>();     // e.g., float32 vector bytes or UTF-8 text
    public byte[] ActPayload = Array.Empty<byte>();
    public byte[]? NextObsPayload;                      // optional
    public string[]? Tags;                              // optional

    // Convenience builders:
    public static ExperienceEnvelope VectorObsDiscreteAct(
        long id, long ticks, int episode, int step, float reward, bool terminal,
        ReadOnlySpan<float> obsVector, int actionIndex, ReadOnlySpan<float> nextObsVector = default)
    {
        var env = new ExperienceEnvelope
        {
            Id = id,
            TicksUtc = ticks,
            Episode = episode,
            Step = step,
            Reward = reward,
            Terminal = terminal,
            ObsType = ObsKind.VectorF32,
            ActType = ActKind.Discrete,
            ObsPayload = FloatVecToBytes(obsVector),
            ActPayload = BitConverter.GetBytes(actionIndex)
        };
        if (!nextObsVector.IsEmpty) env.NextObsPayload = FloatVecToBytes(nextObsVector);
        return env;
    }

    private static byte[] FloatVecToBytes(ReadOnlySpan<float> vec)
    {
        var bytes = new byte[vec.Length * 4];
        MemoryMarshal.Cast<float, byte>(vec).CopyTo(bytes);
        return bytes;
    }
}