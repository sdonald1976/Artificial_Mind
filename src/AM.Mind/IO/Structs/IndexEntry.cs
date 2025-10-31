using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Structs;

// --- Fixed-size index entry (packed) ---
// 32 bytes aligned (we’ll keep it straightforward; if you need absolute 32B, swap Episode->int, pack carefully).
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IndexEntry
{
    public long Id;          // 8
    public long FileOffset;  // 8 (offset to RecLen varint in the .xlog)
    public long TicksUtc;    // 8
    public float Reward;      // 4
    public byte Flags;       // 1 (bit0 Terminal)
    public byte FileId;      // 1 (which chunk file index)
    public ushort Episode;     // 2
                               // Total: 32 bytes
}
