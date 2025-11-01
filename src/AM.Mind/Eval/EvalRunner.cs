using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Eval;

public static class EvalRunner
{
    /// <summary>
    /// Runs N episodes synchronously using CoreMind + IEnvironment.
    /// Returns per-episode stats. (Uses CoreMind.Step's env callback to step the env.)
    /// </summary>
    public static List<EpisodeStats> RunEpisodes(
        CoreMind mind,
        IEnvironment<VectorObs, DiscreteAct> env,
        int episodes,
        int maxStepsPerEpisode = 1000)
    {
        var stats = new List<EpisodeStats>(episodes);

        for (int ep = 1; ep <= episodes; ep++)
        {
            var start = DateTime.UtcNow.Ticks;
            var obs = env.Reset();
            double totalR = 0;
            bool success = false;

            for (int step = 0; step < maxStepsPerEpisode; step++)
            {
                // Capture env result from inside CoreMind.Step callback
                EnvStepResult last = default;
                _ = mind.Step(
                    obs,
                    episode: ep,
                    step: step,
                    ticks: DateTime.UtcNow.Ticks,
                    envStep: a =>
                    {
                        last = env.Step(a);
                        return (last.Reward, last.NextObs, last.Terminal);
                    });

                totalR += last.Reward;
                obs = last.NextObs;
                if (last.Terminal)
                {
                    success = true;
                    break;
                }
            }

            var end = DateTime.UtcNow.Ticks;
            stats.Add(new EpisodeStats(
                Episode: ep,
                Steps: Math.Min(maxStepsPerEpisode, (int)((end - start) / TimeSpan.TicksPerMillisecond) /*approx*/),
                TotalReward: totalR,
                Succeeded: success,
                StartTicksUtc: start,
                EndTicksUtc: end));
        }

        return stats;
    }
}
