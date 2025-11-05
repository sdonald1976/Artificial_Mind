using AM.Mind.Models;
using AM.Mind.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Tools;

public static class TokenizerInspector
{
    public sealed class Summary
    {
        public int VocabSize { get; init; }
        public long TotalPieces { get; init; }
        public int UniquePiecesUsed { get; init; }
        public double AvgPiecesPerWord { get; init; }
        public double PctWords1Piece { get; init; }
        public double PctWords2Pieces { get; init; }
        public double PctWords3Plus { get; init; }
    }

    public static void Analyze(
        string tokenizerJsonPath,
        IEnumerable<string> corpusLines,
        string outDir,
        int topN = 200,
        int sampleWords = 100)
    {
        Directory.CreateDirectory(outDir);

        // Load model + tokenizer
        var model = BpeModel.FromJson(File.ReadAllText(tokenizerJsonPath));
        var tok = new BpeTokenizer(model);

        // Piece frequencies over the corpus
        var pieceFreq = new Dictionary<int, long>();
        long totalPieces = 0;

        // Word segmentation stats
        long words = 0, w1 = 0, w2 = 0, w3p = 0;
        var rnd = new Random(42);
        var wordSamples = new List<string>();

        foreach (var line in corpusLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Count piece frequency by encoding the whole line
            var idsLine = tok.Encode(line, addBos: false, addEos: false);
            foreach (var id in idsLine)
            {
                if (id < 0 || id >= model.Vocab.Size) continue;
                pieceFreq[id] = pieceFreq.TryGetValue(id, out var c) ? c + 1 : 1;
                totalPieces++;
            }

            // Word-level segmentation stats
            var wordsInLine = SplitWords(line);
            foreach (var w in wordsInLine)
            {
                var ids = tok.Encode(w, addBos: false, addEos: false);
                int k = ids.Length;
                words++;
                if (k <= 1) w1++;
                else if (k == 2) w2++;
                else w3p++;

                // random sample a few words to show segmentation examples
                if (wordSamples.Count < sampleWords && rnd.NextDouble() < 0.05)
                    wordSamples.Add(w);
            }
        }

        // Summary
        var usedPieces = pieceFreq.Count;
        var summary = new Summary
        {
            VocabSize = model.Vocab.Size,
            TotalPieces = totalPieces,
            UniquePiecesUsed = usedPieces,
            AvgPiecesPerWord = words == 0 ? 0 : (double)totalPieces / Math.Max(1, words),
            PctWords1Piece = words == 0 ? 0 : (double)w1 / words * 100.0,
            PctWords2Pieces = words == 0 ? 0 : (double)w2 / words * 100.0,
            PctWords3Plus = words == 0 ? 0 : (double)w3p / words * 100.0,
        };

        File.WriteAllText(Path.Combine(outDir, "summary.txt"),
$@"Vocab size:            {summary.VocabSize}
Unique pieces (used):   {summary.UniquePiecesUsed}
Total pieces (corpus):  {summary.TotalPieces}
Avg pieces / word:      {summary.AvgPiecesPerWord:F2}
% words = 1 piece:      {summary.PctWords1Piece:F1}%
% words = 2 pieces:     {summary.PctWords2Pieces:F1}%
% words ≥ 3 pieces:     {summary.PctWords3Plus:F1}%
");

        // Top pieces
        var top = pieceFreq.OrderByDescending(kv => kv.Value).Take(topN).ToArray();
        using (var sw = new StreamWriter(Path.Combine(outDir, "top_pieces.csv"), false, Encoding.UTF8))
        {
            sw.WriteLine("rank,id,piece,freq");
            for (int i = 0; i < top.Length; i++)
            {
                int id = top[i].Key;
                long f = top[i].Value;
                string piece = SafePiece(model.Vocab.IdToToken[id]);
                sw.WriteLine($"{i + 1},{id},\"{piece}\",{f}");
            }
        }

        // Least-used / suspicious pieces
        var tail = pieceFreq.OrderBy(kv => kv.Value).Take(Math.Min(200, pieceFreq.Count)).ToArray();
        using (var sw = new StreamWriter(Path.Combine(outDir, "rare_pieces.csv"), false, Encoding.UTF8))
        {
            sw.WriteLine("id,piece,freq");
            foreach (var kv in tail)
            {
                sw.WriteLine($"{kv.Key},\"{SafePiece(model.Vocab.IdToToken[kv.Key])}\",{kv.Value}");
            }
        }

        // Word segmentation samples
        using (var sw = new StreamWriter(Path.Combine(outDir, "word_segments.csv"), false, Encoding.UTF8))
        {
            sw.WriteLine("word,pieces");
            foreach (var w in wordSamples.Distinct().Take(sampleWords))
            {
                var ids = tok.Encode(w, addBos: false, addEos: false);
                var pieces = ids.Select(id => SafePiece(model.Vocab.IdToToken[id]));
                sw.WriteLine($"\"{w}\",\"{string.Join(" + ", pieces)}\"");
            }
        }
    }

    private static IEnumerable<string> SplitWords(string line)
    {
        int i = 0, n = line.Length;
        while (i < n)
        {
            while (i < n && char.IsWhiteSpace(line, i)) i++;
            if (i >= n) yield break;
            int j = i;
            while (j < n && !char.IsWhiteSpace(line, j)) j++;
            yield return line[i..j];
            i = j;
        }
    }

    private static string SafePiece(string s)
        => s.Replace("\"", "\"\"").Replace("\n", "\\n").Replace("\r", "\\r");
}