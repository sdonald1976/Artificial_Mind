using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Adapters;

/// <summary>
/// In-RAM ring buffer that stores recent observations and
/// serves them as context. Also acts as a sink (Append).
/// </summary>
public sealed class RingRecall<TObs, TAct> : IRecall<TObs>, IExperienceSink<TObs, TAct>
{
    private readonly TObs[] _ring;
    private int _count;
    private int _write;

    public RingRecall(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _ring = new TObs[capacity];
        _count = 0;
        _write = 0;
    }

    // IExperienceSink<TObs,TAct>
    public void Append(in Experience<TObs, TAct> exp)
    {
        _ring[_write] = exp.Obs;
        _write = (_write + 1) % _ring.Length;
        if (_count < _ring.Length) _count++;
    }

    // IRecall<TObs>
    public int GetRecent(Span<TObs> destination)
    {
        int n = Math.Min(destination.Length, _count);
        for (int i = 0; i < n; i++)
        {
            int idx = (_write - 1 - i + _ring.Length) % _ring.Length;
            destination[i] = _ring[idx];
        }
        return n;
    }

    // Some compilers/projects don’t allow default interface impls.
    // Provide an explicit stub to satisfy the interface.
    public int QueryByCue(ReadOnlySpan<float> cueVec, Span<TObs> destination, int k) => 0;
}