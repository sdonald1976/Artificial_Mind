using AM.Mind.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.LM;

public static class NGramIO
{
    // Save/Load model
    public static void Save(NGramLm lm, string path) => File.WriteAllText(path, lm.ToJson(true));
    public static NGramLm Load(string path) => NGramLm.FromJson(File.ReadAllText(path));

    // Turn text lines into token id sequences (using your tokenizer)
    public static IEnumerable<int[]> EncodeLines(ITokenizer tok, IEnumerable<string> lines, bool addBos = false, bool addEos = false)
        => lines.Select(s => tok.Encode(s, addBos, addEos));
}
