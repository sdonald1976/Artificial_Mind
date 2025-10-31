using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Enums;

[Flags]
public enum XLogFlags : ushort
{
    None = 0,
    LZ4 = 1 << 0
}
