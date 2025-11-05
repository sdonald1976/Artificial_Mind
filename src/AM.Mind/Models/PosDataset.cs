using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Models;

public static class PosDataset
{
    // Expects files where sentences are separated by blank lines.
    // Each non-empty line: TOKEN[TAB]TAG
    public static List<PosSentence> LoadTsv(string path)
    {
        var sents = new List<PosSentence>();
        var tokens = new List<string>();
        var tags = new List<string>();

        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.TrimEnd();
            if (line.Length == 0)
            {
                if (tokens.Count > 0)
                {
                    sents.Add(new PosSentence(tokens.ToArray(), tags.ToArray()));
                    tokens.Clear(); tags.Clear();
                }
                continue;
            }
            var tab = line.IndexOf('\t');
            if (tab <= 0) continue;
            tokens.Add(line[..tab]);
            tags.Add(line[(tab + 1)..]);
        }
        if (tokens.Count > 0)
            sents.Add(new PosSentence(tokens.ToArray(), tags.ToArray()));
        return sents;
    }
}