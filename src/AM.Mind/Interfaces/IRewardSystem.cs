using AM.Mind.Records;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

public interface IRewardSystem<VectorObs, DiscreteAct>
{
    float Evaluate(in Experience<VectorObs, DiscreteAct> e);
}
