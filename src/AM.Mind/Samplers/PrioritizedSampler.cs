using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Samplers;

public static class PrioritizedSampler
{
    // Mix: α priority(|reward|), (1-α) recency
    public static void SampleTopK(IReadOnlyList<Experience<VectorObs, DiscreteAct>> batch, int k, float alpha,
                                  List<Experience<VectorObs, DiscreteAct>> outList)
    {
        outList.Clear();
        var scored = new List<(float score, int idx)>(batch.Count);
        int n = batch.Count;
        for (int i = 0; i < n; i++)
        {
            var e = batch[i];
            float pr = Math.Abs(e.Reward);
            float rec = (float)i / n; // assume batch ordered old→new; newer → larger rec
            float s = alpha * pr + (1 - alpha) * rec;
            scored.Add((s, i));
        }
        scored.Sort((a, b) => b.score.CompareTo(a.score));
        for (int i = 0; i < Math.Min(k, scored.Count); i++)
            outList.Add(batch[scored[i].idx]);
    }
}