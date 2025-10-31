using AM.Mind.Interfaces;
using AM.Mind.Models;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Training;

public static class ReplayTrainer
{
    public static void TrainFromLog(
        string xlogPath,
        ILearner<VectorObs, DiscreteAct> learner,
        long fromTicks,
        long toTicks,
        int batchSize = 256)
    {
        using var source = new XLogExperienceSource(xlogPath);
        var batch = new List<Experience<VectorObs, DiscreteAct>>(batchSize);

        foreach (var e in source.ReadRange(fromTicks, toTicks))
        {
            batch.Add(e);
            if (batch.Count >= batchSize)
            {
                learner.Learn(batch);
                batch.Clear();
            }
        }
        if (batch.Count > 0) learner.Learn(batch);
    }
}