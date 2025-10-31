using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

// Optional: Policy snapshot export/import
public interface ISnapshottablePolicy<TSnapshot>
{
    TSnapshot Export();
    void Import(TSnapshot snapshot);
}
