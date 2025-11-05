using AM.Mind.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.WSD;

public static class WsdTrainer
{
    public static (double loss, double acc) TrainEpoch(
        BiEncoderWsd model,
        List<WsdExample> train,
        ISenseInventory inv,
        float lr = 0.05f,
        int maxCandidates = 16)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        double lossSum = 0; long correct = 0, total = 0;
        int n = train.Count, lastLog = 0;

        foreach (var ex in train)
        {
            var cands = inv.GetCandidates(ex.Lemma, ex.Pos);
            if (cands.Count == 0) continue;

            // Build gloss list and find gold index
            int gold = -1;
            int m = Math.Min(cands.Count, maxCandidates);
            var glosses = new string[m];
            for (int i = 0; i < m; i++)
            {
                glosses[i] = cands[i].Gloss;
                if (cands[i].SynsetId == ex.GoldSynsetId) gold = i;
            }
            if (gold < 0) continue; // skip if gold not in first m; you can add sampling/shuffling later

            float loss = model.TrainStep(ex.Tokens, ex.TargetIndex, glosses, gold, lr);
            lossSum += loss;

            int pred = model.Predict(ex.Tokens, ex.TargetIndex, glosses);
            if (pred == gold) correct++;
            total++;

            if (total - lastLog >= 1000)
            {
                lastLog = (int)total;
                var t = sw.Elapsed.TotalSeconds;
                var rate = total / Math.Max(1.0, t);                 // ex/s
                var remaining = n - total;
                var etaSec = remaining / Math.Max(1.0, rate);
                Console.WriteLine($"WSD: {total}/{n}  {rate:F1} ex/s  ETA ~ {TimeSpan.FromSeconds(etaSec):hh\\:mm\\:ss}");
            }
        }

        return (total == 0 ? 0 : lossSum / total, total == 0 ? 0 : (double)correct / total);
    }

    public static double Evaluate(
        BiEncoderWsd model,
        List<WsdExample> test,
        ISenseInventory inv,
        int maxCandidates = 16)
    {
        long correct = 0, total = 0;

        foreach (var ex in test)
        {
            var cands = inv.GetCandidates(ex.Lemma, ex.Pos);
            if (cands.Count == 0) continue;

            int m = Math.Min(cands.Count, maxCandidates);
            var glosses = new string[m];
            int gold = -1;
            for (int i = 0; i < m; i++)
            {
                glosses[i] = cands[i].Gloss;
                if (cands[i].SynsetId == ex.GoldSynsetId) gold = i;
            }
            if (gold < 0) continue;

            int pred = model.Predict(ex.Tokens, ex.TargetIndex, glosses);
            if (pred == gold) correct++;
            total++;
        }

        return total == 0 ? 0 : (double)correct / total;
    }
}