using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Learners;

public sealed class OnlineLearner : ILearner<VectorObs, DiscreteAct>
{
    private readonly IPolicy<VectorObs, DiscreteAct> _policy;

    public OnlineLearner(IPolicy<VectorObs, DiscreteAct> policy)
        => _policy = policy ?? throw new System.ArgumentNullException(nameof(policy));

    public void Learn(IEnumerable<Experience<VectorObs, DiscreteAct>> batch)
    {
        foreach (var e in batch)
            _policy.Update(e.Obs, e.Act, e.Reward);
    }
}
