using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Records;

// What the brain emits each step (you already agreed on this shape conceptually).
// Ground-truth unit we log each step of life.
public readonly record struct Experience<TObs, TAct>(
    TObs Obs,
    TAct Act,
    float Reward,
    TObs? NextObs = default,
    bool Terminal = false,
    long Ticks = 0,
    int Episode = 0,
    int Step = 0
);

