using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Rewards;

public sealed class CompositeReward : IRewardSystem<VectorObs, DiscreteAct>
{
    private readonly List<IRewardSystem<VectorObs, DiscreteAct>> _systems = new();
    public CompositeReward Add(IRewardSystem<VectorObs, DiscreteAct> rs) { _systems.Add(rs); return this; }

    public float Evaluate(in Experience<VectorObs, DiscreteAct> e)
    {
        float r = 0f;
        for (int i = 0; i < _systems.Count; i++) r += _systems[i].Evaluate(e);
        return r;
    }
}
