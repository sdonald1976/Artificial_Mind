using AM.Mind.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Util;

public sealed class Rng : IRng
{
    private readonly Random _r;
    public Rng(int seed) => _r = new Random(seed);

    public int NextInt(int maxExclusive) => _r.Next(maxExclusive);
    public double NextDouble() => _r.NextDouble();

    public double NextGaussian()
    {
        double u1 = 1.0 - _r.NextDouble(), u2 = 1.0 - _r.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
