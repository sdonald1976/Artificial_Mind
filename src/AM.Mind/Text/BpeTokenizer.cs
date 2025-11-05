using AM.Mind.Interfaces;
using AM.Mind.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Text;

/// <summary>
/// Trainable Byte-Pair Encoding (BPE) tokenizer with Unicode-safe chars (Rune).
/// - Train: from raw text lines → merges + vocab (with frequency filter).
/// - Encode: whitespace tokenization → per-token BPE merges → ids.
/// - Decode: ids → tokens → join with spaces (specials can be stripped).
/// </summary>
public sealed class BpeTokenizer : ITokenizer
{
    private readonly Vocab _vocab;
    private readonly Dictionary<(string, string), int> _mergeRank; // pair → rank (lower is earlier)
    private readonly string _pad, _unk, _bos, _eos;

    public int VocabSize => _vocab.Size;
    public int PadId => _vocab.PadId;
    public int UnkId => _vocab.UnkId;
    public int BosId => _vocab.BosId;
    public int EosId => _vocab.EosId;

    public BpeTokenizer(BpeModel model)
    {
        if (model.Version != "bpe-v1") throw new InvalidOperationException($"Unsupported tokenizer version: {model.Version}");
        _vocab = model.Vocab ?? throw new ArgumentNullException(nameof(model.Vocab));
        _vocab.RebuildMap();

        _pad = Vocab.DefaultPad; _unk = Vocab.DefaultUnk; _bos = Vocab.DefaultBos; _eos = Vocab.DefaultEos;

        _mergeRank = new Dictionary<(string, string), int>();
        for (int i = 0; i < model.Merges.Count; i++)
            _mergeRank[model.Merges[i]] = i;
    }

    // -------- Public API --------
    public int[] Encode(string text, bool addBos = false, bool addEos = false)
    {
        if (string.IsNullOrEmpty(text))
        {
            var empty = new List<int>(addBos || addEos ? 2 : 0);
            if (addBos) empty.Add(BosId);
            if (addEos) empty.Add(EosId);
            return empty.ToArray();
        }

        text = TextNormalizer.NormalizeForBpe(text);

        var ids = new List<int>(text.Length + 8);
        if (addBos) ids.Add(BosId);

        foreach (var word in SimpleWhitespaceSplit(text))
        {
            var w = TextNormalizer.NormalizeWord(word);
            if (w.Length == 0) continue;

            foreach (var piece in ApplyBpe(w))
                ids.Add(_vocab[piece]);
        }

        if (addEos) ids.Add(EosId);
        return ids.ToArray();
    }

    public string Decode(int[] ids, bool stripSpecials = true)
    {
        if (ids is null || ids.Length == 0) return string.Empty;
        var sb = new StringBuilder();
        for (int i = 0; i < ids.Length; i++)
        {
            var tok = _vocab[ids[i]];
            if (stripSpecials && (tok == _pad || tok == _unk || tok == _bos || tok == _eos))
                continue;
            // Words were tokenized by whitespace; BPE pieces are plain strings here; join with space
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(tok);
        }
        return sb.ToString();
    }

    // -------- BPE internals --------

    private IEnumerable<string> ApplyBpe(string word)
    {
        // Start with character-level symbols (Unicode-safe)
        var symbols = new List<string>(word.Length);
        foreach (var r in EnumerateRunes(word))
            symbols.Add(r.ToString());

        if (symbols.Count == 0) yield break;

        // Greedy merges by ranked pairs
        while (true)
        {
            var pairs = GetPairs(symbols);
            if (pairs.Count == 0) break;

            (int i, (string, string) pair, int rank)? best = null;
            for (int i = 0; i < pairs.Count; i++)
            {
                var p = pairs[i];
                if (_mergeRank.TryGetValue(p, out var rank))
                {
                    if (best is null || rank < best.Value.rank) best = (i, p, rank);
                }
            }
            if (best is null) break;

            // Merge the best pair across the sequence
            var target = best.Value.pair;
            var merged = new List<string>(symbols.Count);
            int idx = 0;
            while (idx < symbols.Count)
            {
                int j = (idx < symbols.Count - 1 && symbols[idx] == target.Item1 && symbols[idx + 1] == target.Item2) ? 1 : 0;
                if (j == 1)
                {
                    merged.Add(symbols[idx] + symbols[idx + 1]);
                    idx += 2;
                }
                else
                {
                    merged.Add(symbols[idx]);
                    idx++;
                }
            }
            symbols = merged;
        }

        // Return final pieces
        for (int i = 0; i < symbols.Count; i++)
            yield return symbols[i];
    }

    private static List<(string, string)> GetPairs(List<string> symbols)
    {
        var pairs = new List<(string, string)>(Math.Max(0, symbols.Count - 1));
        for (int i = 0; i < symbols.Count - 1; i++)
            pairs.Add((symbols[i], symbols[i + 1]));
        return pairs;
    }

    private static IEnumerable<System.Text.Rune> EnumerateRunes(string s)
    {
        var e = s.EnumerateRunes();
        foreach (var r in e) yield return r;
    }

    private static IEnumerable<string> SimpleWhitespaceSplit(string text)
    {
        int i = 0, n = text.Length;
        while (i < n)
        {
            // skip space
            while (i < n && char.IsWhiteSpace(text, i)) i++;
            if (i >= n) yield break;
            int j = i;
            while (j < n && !char.IsWhiteSpace(text, j)) j++;
            yield return text[i..j];
            i = j;
        }
    }

    // -------- Trainer --------

    public static BpeModel Train(
    IEnumerable<string> lines,
    int vocabSize = 8192,
    int minCount = 2,
    Action<string>? log = null,
    Action<BpeModel, int>? onMergeCheckpoint = null,
    int checkpointEvery = 2000) // was 50 → fewer disk hits
    {
        log ??= _ => { };
        log("BPE: building corpus…");

        if (vocabSize < 256) throw new ArgumentOutOfRangeException(nameof(vocabSize), "vocabSize should be >= 256");

        // ---------- Step 1: build unique-word corpus with counts ----------
        // Key = List<string> (characters), Value = count.
        // IMPORTANT: use TryGetValue with the custom comparer instead of scanning Keys.
        var corpus = new Dictionary<List<string>, int>(capacity: 1 << 15, comparer: new ListComparer());

        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var norm = TextNormalizer.NormalizeForBpe(raw);
            if (norm.Length == 0) continue;

            foreach (var word in SimpleWhitespaceSplit(norm))
            {
                var clean = TextNormalizer.NormalizeWord(word);
                if (string.IsNullOrEmpty(clean)) continue;

                var symbols = new List<string>(clean.Length);
                foreach (var r in EnumerateRunes(clean))
                    symbols.Add(r.ToString());

                if (symbols.Count == 0) continue;

                // Fast accumulate (no O(N) scan of Keys)
                if (corpus.TryGetValue(symbols, out var cnt))
                    corpus[symbols] = cnt + 1;
                else
                    corpus[symbols] = 1;
            }
        }

        // ---------- Step 2: base vocab (characters) with frequency filter ----------
        var tokenFreq = new Dictionary<string, int>(capacity: 4096, comparer: StringComparer.Ordinal);
        foreach (var kv in corpus)
        {
            var symList = kv.Key; var count = kv.Value;
            for (int i = 0; i < symList.Count; i++)
            {
                var s = symList[i];
                tokenFreq[s] = tokenFreq.TryGetValue(s, out var f) ? f + count : count;
            }
        }

        log($"BPE: unique words={corpus.Count:N0}  base pieces={tokenFreq.Count:N0}");

        var pieces = new List<string>(tokenFreq.Count);
        foreach (var kv in tokenFreq) if (kv.Value >= minCount) pieces.Add(kv.Key);

        // ---------- Step 3: iterative merges ----------
        var merges = new List<(string left, string right)>(capacity: Math.Max(0, vocabSize - pieces.Count));

        // Preallocate once and reuse pair frequency map per-iteration
        var pairFreq = new Dictionary<(string, string), long>(capacity: 1 << 16);

        while (pieces.Count + merges.Count + 4 /*specials*/ < vocabSize)
        {
            pairFreq.Clear();

            // Count pair frequencies (weighted by word count)
            foreach (var kv in corpus)
            {
                var symList = kv.Key;
                var count = (long)kv.Value;
                // fast scan of adjacent pairs
                for (int i = 0; i < symList.Count - 1; i++)
                {
                    var pair = (symList[i], symList[i + 1]);
                    pairFreq[pair] = pairFreq.TryGetValue(pair, out var f) ? f + count : count;
                }
            }

            if (pairFreq.Count == 0) break;

            // Find best pair (max freq)
            (string A, string B) bestPair = default;
            long bestFreq = 0;
            foreach (var kv in pairFreq)
            {
                if (kv.Value > bestFreq) { bestFreq = kv.Value; bestPair = kv.Key; }
            }
            if (bestFreq < minCount) break;

            if (merges.Count % 500 == 0)
                log($"BPE: merges={merges.Count:N0}  vocab≈{pieces.Count + merges.Count + 4} / {vocabSize}");

            var a = bestPair.A;
            var b = bestPair.B;
            var mergedSym = a + b;

            merges.Add((a, b));
            pieces.Add(mergedSym);

            // Replace (a,b) → mergedSym in every word (aggregate identical results)
            var newCorpus = new Dictionary<List<string>, int>(capacity: corpus.Count, comparer: new ListComparer());
            foreach (var kv in corpus)
            {
                var src = kv.Key; var count = kv.Value;
                var outList = new List<string>(src.Count);

                for (int i = 0; i < src.Count;)
                {
                    if (i < src.Count - 1 && ReferenceEqualsOrEqual(src[i], a) && ReferenceEqualsOrEqual(src[i + 1], b))
                    {
                        outList.Add(mergedSym);
                        i += 2;
                    }
                    else
                    {
                        outList.Add(src[i]);
                        i++;
                    }
                }

                if (newCorpus.TryGetValue(outList, out var acc))
                    newCorpus[outList] = acc + count;
                else
                    newCorpus[outList] = count;
            }

            corpus = newCorpus;

            // checkpoint
            if (onMergeCheckpoint != null && checkpointEvery > 0 && (merges.Count % checkpointEvery) == 0)
            {
                var ckVocab = Vocab.FromTokens(pieces);
                onMergeCheckpoint(new BpeModel { Version = "bpe-v1", Vocab = ckVocab, Merges = new List<(string, string)>(merges) }, merges.Count);
            }

            // Safety valve (should never trigger in normal runs)
            if (merges.Count > vocabSize * 2) break;
        }

        // ---------- Finalize ----------
        var finalVocab = Vocab.FromTokens(pieces);
        var model = new BpeModel { Version = "bpe-v1", Vocab = finalVocab, Merges = merges };
        log($"BPE: done. merges={merges.Count:N0}  final vocab={finalVocab.Size:N0}");
        return model;

        // Local fast equality for strings (avoid String.Equals allocation paths)
        static bool ReferenceEqualsOrEqual(string x, string y)
            => ReferenceEquals(x, y) || string.Equals(x, y, StringComparison.Ordinal);
    }

    //public static BpeModel Train(IEnumerable<string> lines, int vocabSize = 8192, int minCount = 2, Action<string>? log = null, Action<BpeModel, int>? onMergeCheckpoint = null, int checkpointEvery = 50)
    //{
    //    log ??= _ => { };
    //    log($"BPE: building corpus…");

    //    if (vocabSize < 256) throw new ArgumentOutOfRangeException(nameof(vocabSize), "vocabSize should be >= 256");

    //    // Step 1: build initial "word as chars" corpus with counts
    //    var corpus = new Dictionary<List<string>, int>(new ListComparer());
    //    foreach (var line in lines)
    //    {
    //        if (string.IsNullOrWhiteSpace(line)) continue;

    //        var norm = TextNormalizer.NormalizeForBpe(line);

    //        foreach (var word in SimpleWhitespaceSplit(norm))
    //        {
    //            var clean = TextNormalizer.NormalizeWord(word);
    //            if (string.IsNullOrEmpty(clean)) continue;

    //            var symbols = new List<string>(clean.Length);
    //            foreach (var r in EnumerateRunes(clean))
    //                symbols.Add(r.ToString());

    //            if (symbols.Count == 0) continue;
    //            // Count
    //            bool found = false;
    //            foreach (var kv in corpus.Keys)
    //            {
    //                if (ListComparer.SequenceEqual(kv, symbols)) { corpus[kv] = corpus[kv] + 1; found = true; break; }
    //            }
    //            if (!found) corpus[symbols] = 1;
    //        }
    //    }

    //    // Step 2: build base vocab (characters) with frequency filter
    //    var tokenFreq = new Dictionary<string, int>(StringComparer.Ordinal);
    //    foreach (var (symList, count) in corpus)
    //        foreach (var s in symList)
    //            tokenFreq[s] = tokenFreq.TryGetValue(s, out var f) ? f + count : count;

    //    log($"BPE: unique words={corpus.Count:N0}  base pieces={tokenFreq.Count:N0}");

    //    var pieces = tokenFreq.Where(kv => kv.Value >= minCount).Select(kv => kv.Key).ToList();

    //    // Step 3: iterative merges
    //    var merges = new List<(string, string)>();
    //    while (pieces.Count + merges.Count + 4 /*specials*/ < vocabSize)
    //    {
    //        // Count pair frequencies
    //        var pairFreq = new Dictionary<(string, string), int>();
    //        foreach (var (symList, count) in corpus)
    //        {
    //            for (int i = 0; i < symList.Count - 1; i++)
    //            {
    //                var pair = (symList[i], symList[i + 1]);
    //                pairFreq[pair] = pairFreq.TryGetValue(pair, out var f) ? f + count : count;
    //            }
    //        }

    //        if (pairFreq.Count == 0) break;

    //        // Best pair by freq
    //        var best = pairFreq.OrderByDescending(kv => kv.Value).First();
    //        if (best.Value < minCount) break;

    //        if (merges.Count % 100 == 0)
    //            log($"BPE: merges={merges.Count:N0}  vocab≈{pieces.Count + merges.Count + 4} / {vocabSize}");

    //        var (a, b) = best.Key;
    //        var merged = a + b;
    //        merges.Add(best.Key);
    //        pieces.Add(merged);

    //        // Replace pair (a,b) with merged token in corpus
    //        var newCorpus = new Dictionary<List<string>, int>(new ListComparer());
    //        foreach (var (symList, count) in corpus)
    //        {
    //            var outList = new List<string>(symList.Count);
    //            int i = 0;
    //            while (i < symList.Count)
    //            {
    //                if (i < symList.Count - 1 && symList[i] == a && symList[i + 1] == b)
    //                {
    //                    outList.Add(merged);
    //                    i += 2;
    //                }
    //                else
    //                {
    //                    outList.Add(symList[i]);
    //                    i++;
    //                }
    //            }
    //            // merge identical lists to keep counts aggregated
    //            bool found = false;
    //            foreach (var key in newCorpus.Keys)
    //            {
    //                if (ListComparer.SequenceEqual(key, outList)) { newCorpus[key] = newCorpus[key] + count; found = true; break; }
    //            }
    //            if (!found) newCorpus[outList] = count;
    //        }
    //        corpus = newCorpus;

    //        // checkpoint (incremental save)
    //        if (onMergeCheckpoint != null && checkpointEvery > 0 && (merges.Count % checkpointEvery) == 0)
    //        {
    //            // Note: intermediate vocab = current base pieces + performed merges (+specials when serialized)
    //            var ckVocab = Vocab.FromTokens(pieces);
    //            onMergeCheckpoint(new BpeModel { Version = "bpe-v1", Vocab = ckVocab, Merges = new List<(string, string)>(merges) }, merges.Count);
    //        }

    //        if (merges.Count > vocabSize * 2) break; // safety
    //    }

    //    // Final vocab: specials + pieces
    //    var vocab = Vocab.FromTokens(pieces);

    //    var finalModel = new BpeModel { Version = "bpe-v1", Vocab = vocab, Merges = merges };

    //    log($"BPE: done. merges={merges.Count:N0}  final vocab={vocab.Size:N0}");

    //    return finalModel;
    //}
    // Helper comparer for List<string> dictionary keys
    private sealed class ListComparer : IEqualityComparer<List<string>>
    {
        public bool Equals(List<string>? x, List<string>? y)
            => SequenceEqual(x, y);

        public int GetHashCode(List<string> obj)
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < obj.Count; i++)
                    h = h * 31 + obj[i].GetHashCode();
                return h;
            }
        }

        public static bool SequenceEqual(List<string>? a, List<string>? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) return false;
            return true;
        }
    }
}