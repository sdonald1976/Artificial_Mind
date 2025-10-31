using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

public interface IPolicy<TObs, TAct>
{
    TAct Decide(in TObs obs, ReadOnlySpan<TObs> recent);
    void Update(in TObs obs, in TAct act, float reward);
}
