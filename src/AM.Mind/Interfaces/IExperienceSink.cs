using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

// Port for *writing* life.
public interface IExperienceSink<TObs, TAct>
{
    // Fire-and-forget is fine for speed; you can expose async later.
    void Append(in Experience<TObs, TAct> exp);
}
