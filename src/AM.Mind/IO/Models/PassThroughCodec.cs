using AM.Mind.IO.Enums;
using AM.Mind.IO.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Models;

public sealed class PassThroughCodec : ICodec
{
    public byte[] Encode(ReadOnlySpan<byte> src) => src.ToArray();
    public byte[] Decode(ReadOnlySpan<byte> src) => src.ToArray();
    public XLogFlags Flags => XLogFlags.None;
}
