using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Models;

public sealed class SenseCandidate
{
    public string SynsetId { get; }
    public string Gloss { get; }
    public string[] Examples { get; }

    public SenseCandidate(string synsetId, string gloss, string[] examples)
    { SynsetId = synsetId; Gloss = gloss; Examples = examples; }
}
