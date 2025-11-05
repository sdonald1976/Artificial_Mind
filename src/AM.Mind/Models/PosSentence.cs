using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Models;

public sealed class PosSentence
{
    public readonly string[] Tokens;
    public readonly string[] Tags;
    public PosSentence(string[] tokens, string[] tags)
    { Tokens = tokens; Tags = tags; }
}
