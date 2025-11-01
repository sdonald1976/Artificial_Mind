using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Records;

// Step result for environments using VectorObs/DiscreteAct
public readonly record struct EnvStepResult(float Reward, VectorObs NextObs, bool Terminal);
