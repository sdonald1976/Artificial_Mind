using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.WSD;

/// <summary>
/// TSV: sentence \t targetIndex \t lemma \t pos \t goldSynsetId
/// sentence is space-separated tokens.
/// </summary>
public static class WsdDataset
{
    public static List<WsdExample> Load(string path)
    {
        var list = new List<WsdExample>();
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            var parts = line.Split('\t');
            if (parts.Length < 5) continue;

            var tokens = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(parts[1], out int ti)) continue;
            var lemma = parts[2].Trim();
            var pos = parts[3].Trim();
            var gold = parts[4].Trim();

            if (ti < 0 || ti >= tokens.Length) continue;
            list.Add(new WsdExample(tokens, ti, lemma, pos, gold));
        }
        return list;
    }
}