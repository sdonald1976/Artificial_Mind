using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

public interface IEnvironment<TObs, TAct>
{
    // Set to a fresh episode; returns the initial observation
    TObs Reset();

    // Apply an action; returns (reward, nextObs, terminal)
    EnvStepResult Step(TAct act);
}
