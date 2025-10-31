using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

// Optional: offline replay for learners/analysis; the *mind* itself doesn’t need this to run.
public interface IExperienceSource<TObs, TAct>
{
    // Iterate a time/episode window, or ids. Can be a blocking enumerator.
    IEnumerable<Experience<TObs, TAct>> ReadRange(long fromTicks, long toTicks);
}
