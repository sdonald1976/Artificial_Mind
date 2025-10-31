using AM.Mind.Interfaces;
using AM.Mind.IO;
using AM.Mind.IO.Enums;
using AM.Mind.IO.Models;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Models;

public sealed class XLogExperienceSink : IExperienceSink<VectorObs, DiscreteAct>
{
    private readonly XLogWriter _writer;

    public XLogExperienceSink(XLogWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public void Append(in Experience<VectorObs, DiscreteAct> exp)
    {
        // Build envelope
        var env = new ExperienceEnvelope
        {
            Id = 0,                       // writer will assign if you want, or pass exp.Step
            TicksUtc = exp.Ticks,
            Episode = exp.Episode,
            Step = exp.Step,
            Reward = exp.Reward,
            Terminal = exp.Terminal,
            ObsType = ObsKind.VectorF32,
            ActType = ActKind.Discrete,
            ObsPayload = FloatVecToBytes(exp.Obs.Features.Span),
            ActPayload = BitConverter.GetBytes(exp.Act.Index),
            NextObsPayload = FloatVecToBytes(exp.NextObs.Features.Span) ?? null
        };

        _writer.Append(env);
    }

    private static byte[] FloatVecToBytes(ReadOnlySpan<float> vec)
    {
        var bytes = new byte[vec.Length * sizeof(float)];
        System.Runtime.InteropServices.MemoryMarshal.Cast<float, byte>(vec).CopyTo(bytes);
        return bytes;
    }
}