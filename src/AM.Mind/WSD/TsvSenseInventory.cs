using AM.Mind.Interfaces;
using AM.Mind.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.WSD;

/// <summary>
/// Simple TSV inventory:
/// lemma \t pos \t synsetId \t gloss \t example1 | example2 | ...
/// </summary>
public sealed class TsvSenseInventory : ISenseInventory
{
    private readonly Dictionary<(string lemma, string pos), List<SenseCandidate>> _map
        = new(StringTupleComparer.Ordinal);

    public static TsvSenseInventory Load(string path)
    {
        var inv = new TsvSenseInventory();
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            var parts = line.Split('\t');
            if (parts.Length < 4) continue;

            string lemma = parts[0].Trim();
            string pos = parts[1].Trim();
            string syn = parts[2].Trim();
            string gloss = parts[3].Trim();
            string[] ex = parts.Length >= 5 ? parts[4].Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();

            var key = (lemma, pos);
            if (!inv._map.TryGetValue(key, out var list)) { list = new List<SenseCandidate>(); inv._map[key] = list; }
            list.Add(new SenseCandidate(syn, gloss, ex));
        }
        return inv;
    }

    public List<SenseCandidate> GetCandidates(string lemma, string pos)
    {
        return _map.TryGetValue((lemma, pos), out var list) ? new List<SenseCandidate>(list)
                                                            : new List<SenseCandidate>();
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string, string)>
    {
        public static readonly StringTupleComparer Ordinal = new();
        public bool Equals((string, string) x, (string, string) y)
            => string.Equals(x.Item1, y.Item1, StringComparison.Ordinal) &&
               string.Equals(x.Item2, y.Item2, StringComparison.Ordinal);
        public int GetHashCode((string, string) obj)
        { unchecked { return obj.Item1.GetHashCode() * 397 ^ obj.Item2.GetHashCode(); } }
    }
}