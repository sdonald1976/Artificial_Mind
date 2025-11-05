using AM.Mind.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.LM;

public static class LmSampler
{
    public static string Generate(ITokenizer tok, NGramLm lm, string prompt, int maxNewTokens = 30, int seed = 1234)
    {
        var rnd = new Random(seed);
        var ctx = new List<int>(tok.Encode(prompt, addBos: true, addEos: false));
        for (int t = 0; t < maxNewTokens; t++)
        {
            int next = lm.SampleNext(ctx.ToArray(), rnd);
            if (next == lm.EosId) break;
            ctx.Add(next);
        }
        return tok.Decode(ctx.ToArray(), stripSpecials: true);
    }
}
