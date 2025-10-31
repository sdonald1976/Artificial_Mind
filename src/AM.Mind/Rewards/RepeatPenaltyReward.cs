using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Rewards;

public sealed class RepeatPenaltyReward : IRewardSystem<VectorObs, DiscreteAct>
{
    private readonly int _window;
    private readonly float _penalty;
    private int _lastAct = -1;
    private int _repeatStreak = 0;

    public RepeatPenaltyReward(int window = 4, float penalty = 0.2f)
    { _window = Math.Max(1, window); _penalty = penalty; }

    public float Evaluate(in Experience<VectorObs, DiscreteAct> e)
    {
        if (e.Act.Index == _lastAct) _repeatStreak++;
        else { _lastAct = e.Act.Index; _repeatStreak = 0; }

        float p = (_repeatStreak >= _window) ? _penalty : 0f;
        return e.Reward - p;
    }
}