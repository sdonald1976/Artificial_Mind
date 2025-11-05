using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

public interface ITokenizer
{
    int VocabSize { get; }
    int PadId { get; }
    int UnkId { get; }
    int BosId { get; }
    int EosId { get; }

    int[] Encode(string text, bool addBos = false, bool addEos = false);
    string Decode(int[] ids, bool stripSpecials = true);
}
