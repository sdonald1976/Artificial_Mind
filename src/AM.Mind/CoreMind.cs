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
    private readonly IRewardSystem<VectorObs, DiscreteAct> _reward;

    public CoreMind(
        IPolicy<VectorObs, DiscreteAct> policy,
        IExperienceSink<VectorObs, DiscreteAct> sink,
        IRecall<VectorObs> recall,
        IRewardSystem<VectorObs, DiscreteAct> reward)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _recall = recall ?? throw new ArgumentNullException(nameof(recall));
        _reward = reward ?? throw new ArgumentNullException(nameof(reward));
    }

    /// <summary>
    /// One full step: recall → decide → env → update → log
    /// </summary>
    public DiscreteAct Step(
        in VectorObs obs,
        int episode,
        int step,
        long ticks,
        Func<DiscreteAct, (float reward, VectorObs nextObs, bool terminal)> envStep)
    {
        if (envStep is null) throw new ArgumentNullException(nameof(envStep));

        var recentBuf = new VectorObs[4];
        int n = _recall.GetRecent(recentBuf);

        var act = _policy.Decide(obs, recentBuf.AsSpan(0, n));
        var (rawReward, nextObs, terminal) = envStep(act);

        // Optional reward shaping layer
        var exp = new Experience<VectorObs, DiscreteAct>(obs, act, rawReward, nextObs, terminal, ticks, episode, step);
        float finalReward = _reward.Evaluate(exp);

        _policy.Update(obs, act, finalReward);
        _sink.Append(exp);

        return act;
    }
}