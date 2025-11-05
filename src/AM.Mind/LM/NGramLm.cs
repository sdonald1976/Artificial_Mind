using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AM.Mind.LM;

public sealed class NGramLm
{
    public int Order { get; }
    public int BosId { get; }
    public int EosId { get; }
    public float Discount { get; }   // single discount D for KN (0.7–1.0 works well)

    // Statistics
    private readonly Dictionary<string, int> _countN;         // ngram -> count
    private readonly Dictionary<string, int> _contFollowing;  // context -> #distinct followers (continuation count)
    private readonly Dictionary<int, int> _contPreceding;     // token -> #distinct contexts it follows (for unigram KN)
    private readonly HashSet<string> _seenPairs;              // for continuation tallies
    private long[] _totalByOrder;                             // total ngrams per order

    // Cache for probs
    private readonly Dictionary<string, float> _probCache = new();

    public NGramLm(int order, int bosId, int eosId, float discount = 0.75f)
    {
        if (order < 1 || order > 5) throw new ArgumentOutOfRangeException(nameof(order));
        Order = order; BosId = bosId; EosId = eosId; Discount = discount;

        _countN = new Dictionary<string, int>();
        _contFollowing = new Dictionary<string, int>();
        _contPreceding = new Dictionary<int, int>();
        _seenPairs = new HashSet<string>();
        _totalByOrder = new long[order + 1]; // index by n
    }

    // ---- Training ----
    public void Fit(IEnumerable<int[]> sequences) => Fit(sequences, null);
    public void Fit(IEnumerable<int[]> sequences, Action<long>? onProgress = null)
    {
        long seq = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        const int every = 5_000;

        foreach (var seqArr in sequences)
        {
            // Add BOS^(Order-1) … seq … EOS
            var padded = new List<int>((Order - 1) + seqArr.Length + 1);
            for (int i = 0; i < Order - 1; i++) padded.Add(BosId);
            padded.AddRange(seqArr);
            padded.Add(EosId);

            seq++;
            if (seq % every == 0)
            {
                var t = Math.Max(1e-6, sw.Elapsed.TotalSeconds);
                Console.WriteLine($"NGram Fit: {seq:N0} seq  {seq / t:N1}/s");
            }

            // count all ngrams up to Order
            for (int i = 0; i < padded.Count; i++)
            {
                for (int n = 1; n <= Order; n++)
                {
                    if (i - n + 1 < 0) break;
                    var key = Key(padded, i - n + 1, n);
                    _countN[key] = _countN.TryGetValue(key, out var c) ? c + 1 : 1;
                    _totalByOrder[n]++;

                    if (n >= 2)
                    {
                        // continuation stats (context -> distinct follower; token -> distinct contexts)
                        var ctx = Key(padded, i - n + 1, n - 1);
                        var tok = padded[i];
                        var pairKey = ctx + "|" + tok.ToString();
                        if (_seenPairs.Add(pairKey))
                        {
                            _contFollowing[ctx] = _contFollowing.TryGetValue(ctx, out var u) ? u + 1 : 1;
                            _contPreceding[tok] = _contPreceding.TryGetValue(tok, out var v) ? v + 1 : 1;
                        }
                    }
                }
            }
        }

        Console.WriteLine($"NGram Fit: DONE {seq:N0} seq in {sw.Elapsed.TotalSeconds:0.0}s");
    }

    // ---- Probabilities (Interpolated Kneser–Ney) ----

    public float Prob(int[] context, int token)
    {
        // context length up to Order-1
        int n = Math.Min(Order - 1, context.Length);
        // build keys from rightmost n tokens
        return ProbRecursive(context, token, n);
    }

    private float ProbRecursive(int[] ctxArr, int token, int n)
    {
        // cache key
        var cacheKey = CacheKey(ctxArr, token, n);
        if (_probCache.TryGetValue(cacheKey, out var pv)) return pv;

        float p;
        if (n == 0)
        {
            // Unigram KN: continuation probability = (#contexts token seen in) / (sum over tokens of continuation counts)
            long denom = 0;
            foreach (var kv in _contPreceding) denom += kv.Value;
            if (denom == 0) // very early edge case
                p = 1e-8f;
            else
                p = _contPreceding.TryGetValue(token, out var cont) ? (float)cont / denom : 1e-10f;
        }
        else
        {
            var ctxKey = TailKey(ctxArr, n);
            var ctxCount = _countN.TryGetValue(ctxKey, out var cc) ? cc : 0;

            var ngramKey = ctxKey + " " + token.ToString();
            var cstar = _countN.TryGetValue(ngramKey, out var c) ? c : 0;

            // discounted MLE
            float lambda = 0f;
            float ml = 0f;
            if (ctxCount > 0)
            {
                ml = Math.Max(c - Discount, 0f) / ctxCount;

                // continuation mass proportional to #distinct followers for this context
                int numFollowers = _contFollowing.TryGetValue(ctxKey, out var u) ? u : 0;
                lambda = (Discount * numFollowers) / ctxCount;
            }

            // backoff prob
            p = ml + lambda * ProbRecursive(ctxArr, token, n - 1);
        }

        _probCache[cacheKey] = p;
        return p;
    }

    // ---- Sampling ----

    public int SampleNext(int[] context, Random rng)
    {
        // naive sampling by scanning all tokens from unigram continuation set
        // For speed you’d typically restrict to vocab seen in training. We’ll approximate by scanning unique tokens from _contPreceding.
        if (_contPreceding.Count == 0) return EosId;

        // accumulate distribution, then sample
        double sum = 0;
        foreach (var kv in _contPreceding)
            sum += Prob(context, kv.Key);

        double r = rng.NextDouble() * sum, acc = 0;
        foreach (var kv in _contPreceding)
        {
            acc += Prob(context, kv.Key);
            if (acc >= r) return kv.Key;
        }
        return EosId;
    }

    // ---- Perplexity ----

    public double Perplexity(IEnumerable<int[]> sequences)
    {
        double log2 = Math.Log(2.0);
        double logSum = 0; long tokCount = 0;

        foreach (var seq in sequences)
        {
            var padded = new List<int>((Order - 1) + seq.Length + 1);
            for (int i = 0; i < Order - 1; i++) padded.Add(BosId);
            padded.AddRange(seq);
            padded.Add(EosId);

            for (int i = Order - 1; i < padded.Count; i++)
            {
                int tok = padded[i];
                var ctx = padded.Skip(i - (Order - 1)).Take(Order - 1).ToArray();
                float p = Math.Max(Prob(ctx, tok), 1e-12f);
                logSum += -Math.Log(p) / log2; // bits
                tokCount++;
            }
        }
        return tokCount == 0 ? double.PositiveInfinity : Math.Pow(2.0, logSum / tokCount);
    }

    // ---- Save/Load ----

    public string ToJson(bool indented = false)
    {
        var model = new Serializable
        {
            Order = Order,
            BosId = BosId,
            EosId = EosId,
            Discount = Discount,
            CountN = _countN,
            ContFollowing = _contFollowing,
            ContPreceding = _contPreceding,
            TotalByOrder = _totalByOrder
        };
        return JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = indented });
    }

    public static NGramLm FromJson(string json)
    {
        var m = JsonSerializer.Deserialize<Serializable>(json)!;
        var lm = new NGramLm(m.Order, m.BosId, m.EosId, m.Discount)
        {
            _totalByOrder = m.TotalByOrder ?? new long[m.Order + 1]
        };
        foreach (var kv in m.CountN!) lm._countN[kv.Key] = kv.Value;
        foreach (var kv in m.ContFollowing!) lm._contFollowing[kv.Key] = kv.Value;
        foreach (var kv in m.ContPreceding!) lm._contPreceding[kv.Key] = kv.Value;
        return lm;
    }

    private sealed class Serializable
    {
        public int Order { get; set; }
        public int BosId { get; set; }
        public int EosId { get; set; }
        public float Discount { get; set; }
        public Dictionary<string, int>? CountN { get; set; }
        public Dictionary<string, int>? ContFollowing { get; set; }
        public Dictionary<int, int>? ContPreceding { get; set; }
        public long[]? TotalByOrder { get; set; }
    }

    // ---- Utilities ----
    private static string Key(IReadOnlyList<int> arr, int start, int len)
    {
        if (len == 1) return arr[start].ToString();
        var sb = new System.Text.StringBuilder(len * 3);
        for (int i = 0; i < len; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(arr[start + i]);
        }
        return sb.ToString();
    }

    private static string TailKey(int[] ctxArr, int n)
    {
        var len = ctxArr.Length;
        if (n == 1) return ctxArr[len - 1].ToString();
        var sb = new System.Text.StringBuilder(n * 3);
        for (int i = len - n; i < len; i++)
        {
            if (i > len - n) sb.Append(' ');
            sb.Append(ctxArr[i]);
        }
        return sb.ToString();
    }

    private static string CacheKey(int[] ctxArr, int token, int n)
    {
        if (n == 0) return $"|{token}";
        var sb = new System.Text.StringBuilder(n * 3 + 8);
        for (int i = ctxArr.Length - n; i < ctxArr.Length; i++)
        {
            if (i > ctxArr.Length - n) sb.Append(' ');
            sb.Append(ctxArr[i]);
        }
        sb.Append('|').Append(token);
        return sb.ToString();
    }
}
