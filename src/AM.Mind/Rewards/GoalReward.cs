using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Rewards;

/// <summary> Adds +bonus if the predicate holds for the experience. </summary>
public sealed class GoalReward : IRewardSystem<VectorObs, DiscreteAct>
{
    private readonly Func<Experience<VectorObs, DiscreteAct>, bool> _predicate;
    private readonly float _bonus;

    public GoalReward(Func<Experience<VectorObs, DiscreteAct>, bool> predicate, float bonus = 1.0f)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _bonus = bonus;
    }

    public float Evaluate(in Experience<VectorObs, DiscreteAct> e)
        => e.Reward + (_predicate(e) ? _bonus : 0f);
}
