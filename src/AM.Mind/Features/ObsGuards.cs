using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Features;

public static class ObsGuards
{
    public static void ValidateVector(ReadOnlySpan<float> x, int expectedDim)
    {
        if (x.Length != expectedDim)
            throw new ArgumentException($"VectorObs length {x.Length} != expected {expectedDim}");
        for (int i = 0; i < x.Length; i++)
        {
            float v = x[i];
            if (float.IsNaN(v) || float.IsInfinity(v))
                throw new ArgumentException($"VectorObs contains invalid value at {i}: {v}");
        }
    }
}