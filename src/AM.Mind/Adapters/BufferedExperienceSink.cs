using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AM.Mind.Adapters;

public sealed class BufferedExperienceSink<TObs, TAct> : IExperienceSink<TObs, TAct>, IDisposable
{
    private readonly IExperienceSink<TObs, TAct> _inner;
    private readonly Channel<Experience<TObs, TAct>> _ch;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pump;
    private readonly bool _dropWhenFull;

    // telemetry
    public long Enqueued => _enq;
    public long Dropped => _drop;
    public long Written => _wrote;
    private long _enq, _drop, _wrote;

    public BufferedExperienceSink(IExperienceSink<TObs, TAct> inner, int capacity = 4096, bool dropWhenFull = false)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _dropWhenFull = dropWhenFull;
        var opts = new BoundedChannelOptions(capacity)
        {
            FullMode = dropWhenFull ? BoundedChannelFullMode.DropWrite : BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _ch = Channel.CreateBounded<Experience<TObs, TAct>>(opts);
        _pump = Task.Run(PumpAsync);
    }

    public void Append(in Experience<TObs, TAct> exp)
    {
        if (!_ch.Writer.TryWrite(exp))
        {
            if (_dropWhenFull) Interlocked.Increment(ref _drop);
            else _ch.Writer.WriteAsync(exp, _cts.Token).AsTask().GetAwaiter().GetResult();
            return;
        }
        Interlocked.Increment(ref _enq);
    }

    private async Task PumpAsync()
    {
        try
        {
            while (await _ch.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
            {
                while (_ch.Reader.TryRead(out var e))
                {
                    _inner.Append(e);
                    Interlocked.Increment(ref _wrote);
                }
            }
        }
        catch (OperationCanceledException) { /* normal */ }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _ch.Writer.TryComplete();
        try { _pump.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
        if (_inner is IDisposable d) d.Dispose();
    }
}