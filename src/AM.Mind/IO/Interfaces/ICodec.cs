using AM.Mind.IO.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Interfaces;

// --- Minimal codec hook (pass-through by default) ---
public interface ICodec
{
    byte[] Encode(ReadOnlySpan<byte> src);
    byte[] Decode(ReadOnlySpan<byte> src);
    XLogFlags Flags { get; }
}
