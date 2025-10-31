using AM.Mind.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Models;

// Simple periodic ticker
public sealed class BrainClock : IBrainClock
{
    public async Task RunAsync(TimeSpan period, Func<long, Task> onTick, CancellationToken ct = default)
    {
        if (onTick is null) throw new ArgumentNullException(nameof(onTick));
        long t = 0;
        var sw = Stopwatch.StartNew();
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await onTick(t++).ConfigureAwait(false);
                var ms = period - sw.Elapsed;
                sw.Restart();
                if (ms > TimeSpan.Zero)
                    await Task.Delay(ms, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
    }
}
