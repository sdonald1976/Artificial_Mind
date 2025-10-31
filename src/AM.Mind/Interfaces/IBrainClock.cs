using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

// Ticker / scheduler
public interface IBrainClock
{
    // Calls onTick every period until canceled; returns when canceled.
    Task RunAsync(TimeSpan period, Func<long, Task> onTick, CancellationToken ct = default);
}
