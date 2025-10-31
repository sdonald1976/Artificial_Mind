using AM.Mind.Interfaces;
using AM.Mind.IO.Enums;
using AM.Mind.IO.Models;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Models;

public sealed class XLogExperienceSource : IExperienceSource<VectorObs, DiscreteAct>, IDisposable
{
    private readonly IndexReader _idx;
    private readonly XLogReader _rdr;

    public XLogExperienceSource(string xlogPath)
    {
        _rdr = new XLogReader(xlogPath);
        _idx = new IndexReader(System.IO.Path.ChangeExtension(xlogPath, ".fidx"));
    }

    public IEnumerable<Experience<VectorObs, DiscreteAct>> ReadRange(long fromTicks, long toTicks)
    {
        foreach (var e in _idx.All())
        {
            if (e.TicksUtc < fromTicks || e.TicksUtc > toTicks) continue;

            var env = _rdr.ReadAt(e.FileOffset);
            if (env.ObsType != ObsKind.VectorF32 || env.ActType != ActKind.Discrete) continue;

            var obs = new VectorObs(BytesToFloatVec(env.ObsPayload));

            // Build a non-nullable next observation. If none in log, use empty vector.
            VectorObs next = env.NextObsPayload is null
                ? new VectorObs(ReadOnlyMemory<float>.Empty)
                : new VectorObs(BytesToFloatVec(env.NextObsPayload));

            var act = new DiscreteAct(BitConverter.ToInt32(env.ActPayload, 0));

            yield return new Experience<VectorObs, DiscreteAct>(
                obs, act, env.Reward, next, env.Terminal, env.TicksUtc, env.Episode, env.Step);
        }
    }

    private static ReadOnlyMemory<float> BytesToFloatVec(byte[] bytes)
        => MemoryMarshal.Cast<byte, float>(bytes.AsSpan()).ToArray();

    public void Dispose()
    {
        _idx?.Dispose();
        _rdr?.Dispose();
    }
}