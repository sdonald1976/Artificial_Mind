using AM.Mind.Interfaces;
using AM.Mind.IO;
using AM.Mind.IO.Enums;
using AM.Mind.IO.Models;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Adapters;

public sealed class XLogExperienceSink : IExperienceSink<VectorObs, DiscreteAct>, IDisposable
{
    private readonly XLogWriter _writer;
    private int _sinceFlush = 0;
    private readonly int _flushEvery;

    public XLogExperienceSink(XLogWriter writer, int flushEvery = 64)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _flushEvery = Math.Max(1, flushEvery);
    }

    public void Append(in Experience<VectorObs, DiscreteAct> e)
    {
        byte[]? nextBytes = null;
        // Convention: if NextObs.Features.Length == 0, treat as “no next obs”
        if (e.NextObs.Features.Length > 0)
            nextBytes = ToBytes(e.NextObs.Features.Span);

        var env = new ExperienceEnvelope
        {
            Id = 0,
            TicksUtc = e.Ticks,
            Episode = e.Episode,
            Step = e.Step,
            Reward = e.Reward,
            Terminal = e.Terminal,
            ObsType = ObsKind.VectorF32,
            ActType = ActKind.Discrete,
            ObsPayload = ToBytes(e.Obs.Features.Span),
            ActPayload = BitConverter.GetBytes(e.Act.Index),
            NextObsPayload = nextBytes
        };

        _writer.Append(env);
        if ((++_sinceFlush % _flushEvery) == 0) _writer.Flush();
    }

    private static byte[] ToBytes(ReadOnlySpan<float> v)
    {
        var bytes = new byte[v.Length * sizeof(float)];
        MemoryMarshal.Cast<float, byte>(v).CopyTo(bytes);
        return bytes;
    }

    public void Dispose() => _writer?.Flush();
}