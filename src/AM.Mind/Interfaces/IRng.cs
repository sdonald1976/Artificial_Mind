using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

public interface IRng
{
    int NextInt(int maxExclusive);
    double NextDouble();
    double NextGaussian(); // Box–Muller
}
