using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Rewards;

public sealed class TargetActionReward : IRewardSystem<VectorObs, DiscreteAct>
{
    private readonly int _targetAct;
    private readonly float _bonus;
    public TargetActionReward(int targetAct, float bonus = 0.5f)
    { _targetAct = targetAct; _bonus = bonus; }

    public float Evaluate(in Experience<VectorObs, DiscreteAct> e)
        => e.Reward + (e.Act.Index == _targetAct ? _bonus : 0f);
}
