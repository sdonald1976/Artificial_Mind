using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Models;

public sealed class BasicReward : IRewardSystem<VectorObs, DiscreteAct>
{
    public float Evaluate(in Experience<VectorObs, DiscreteAct> e)
        => e.Reward; // pass-through for now
}
