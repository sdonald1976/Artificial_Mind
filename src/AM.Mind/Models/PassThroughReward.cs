using AM.Mind.Records;
using AM.Mind.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Models;

public sealed class PassThroughReward<TObs, TAct> : IRewardSystem<VectorObs, DiscreteAct>
{
    public float Evaluate(in Experience<VectorObs, DiscreteAct> e)
    {
        return e.Reward;
    }
}
