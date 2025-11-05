using AM.Mind.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

public interface ISenseInventory
{
    // Return all candidate senses for (lemma, pos)
    List<SenseCandidate> GetCandidates(string lemma, string pos);
}
