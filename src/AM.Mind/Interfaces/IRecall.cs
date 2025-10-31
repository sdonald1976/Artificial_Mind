using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;
// Port for *recall* at decision time (fast retrieval; not necessarily ground-truth store).
public interface IRecall<TObs>
{
    // Get the last K observations (or experiences) for short-term context.
    int GetRecent(Span<TObs> destination);

    // Semantic cue → kNN recall. If you don't have vectors yet, return 0.
    int QueryByCue(ReadOnlySpan<float> cueVec, Span<TObs> destination, int k);
}
