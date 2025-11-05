using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.WSD;

public sealed class WsdExample
{
    public string[] Tokens { get; }
    public int TargetIndex { get; }
    public string Lemma { get; }
    public string Pos { get; }
    public string GoldSynsetId { get; }

    public WsdExample(string[] tokens, int targetIndex, string lemma, string pos, string goldSynsetId)
    { Tokens = tokens; TargetIndex = targetIndex; Lemma = lemma; Pos = pos; GoldSynsetId = goldSynsetId; }
}
