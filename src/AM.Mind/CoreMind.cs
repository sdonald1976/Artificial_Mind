using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind;

public sealed class CoreMind
{
    private readonly IPolicy<VectorObs, DiscreteAct> _policy;
    private readonly IExperienceSink<VectorObs, DiscreteAct> _sink;
    private readonly IRecall<VectorObs> _recall;

    public CoreMind(
        IPolicy<VectorObs, DiscreteAct> policy,
        IExperienceSink<VectorObs, DiscreteAct> sink,
        IRecall<VectorObs> recall)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _sink   = sink   ?? throw new ArgumentNullException(nameof(sink));
        _recall = recall ?? throw new ArgumentNullException(nameof(recall));
    }

    /// <summary>
    /// One full sense→decide→act→learn→log step.
    /// The envStep func executes the environment and returns (reward, nextObs, terminal).
    /// </summary>
    public DiscreteAct Step(
        in VectorObs obs,
        int episode,
        int step,
        long ticks,
        Func<DiscreteAct, (float reward, VectorObs nextObs, bool terminal)> envStep)
    {
        if (envStep is null) throw new ArgumentNullException(nameof(envStep));

        // NOTE: we can't stackalloc managed types, so use a small managed buffer.
        var recentBuf = new VectorObs[4];
        int n = _recall.GetRecent(recentBuf);

        var act = _policy.Decide(obs, recentBuf.AsSpan(0, n));
        var (reward, nextObs, terminal) = envStep(act);

        _policy.Update(obs, act, reward);

        _sink.Append(new Experience<VectorObs, DiscreteAct>(
            obs, act, reward, nextObs, terminal, ticks, episode, step));

        return act;
    }
}