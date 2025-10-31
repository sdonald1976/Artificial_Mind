using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

public interface IEnvironment
{
    // Provide current state as a fixed-length feature vector.
    float[] GetState();
    // Execute chosen action; returns scalar reward (can be delayed/estimated).
    float Step(int actionIndex);
    // Optional: termination hook.
    bool IsDone { get; }
}
