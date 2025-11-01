using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Env;

public static class EnvAdapters
{
    public static Func<DiscreteAct, (float reward, VectorObs nextObs, bool terminal)>
        AsFunc(IEnvironment<VectorObs, DiscreteAct> env)
        => a =>
        {
            var r = env.Step(a);
            return (r.Reward, r.NextObs, r.Terminal);
        };
}
