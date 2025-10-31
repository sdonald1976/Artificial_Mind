using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Models;

internal static class Constants
{
    public static ReadOnlySpan<byte> Magic => "XLOG"u8;   // 4 bytes
    public const ushort Version = 1;
    public const int HeaderBytes = 4 + 2 + 2 + 8;         // Magic + Version + Flags + Reserved(8)
}
