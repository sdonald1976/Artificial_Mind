using AM.Mind.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Text;

/// <summary>
/// Simple feature hashing encoder (whitespace/punct tokenization + optional char-ngrams).
/// Produces a bag-of-features vector of length FeatureCount.
/// </summary>
public sealed class HashedTextEncoder : ITextEncoder
{
    public int FeatureCount { get; }
    private readonly int _ngramMin, _ngramMax;
    private readonly bool _useCharNgrams;

    public HashedTextEncoder(int featureCount = 8192, int ngramMin = 1, int ngramMax = 2, bool useCharNgrams = false)
    {
        if (featureCount <= 0) throw new ArgumentOutOfRangeException(nameof(featureCount));
        if (ngramMin <= 0 || ngramMax < ngramMin) throw new ArgumentOutOfRangeException(nameof(ngramMin));
        FeatureCount = featureCount;
        _ngramMin = ngramMin;
        _ngramMax = ngramMax;
        _useCharNgrams = useCharNgrams;
    }

    public void Encode(string text, float[] dst)
    {
        Array.Clear(dst, 0, dst.Length);
        if (string.IsNullOrWhiteSpace(text)) return;

        var tokens = Tokenize(text);
        // word ngrams
        for (int n = _ngramMin; n <= _ngramMax; n++)
        {
            for (int i = 0; i + n <= tokens.Count; i++)
            {
                var h = 2166136261u;
                for (int j = 0; j < n; j++)
                    h = HashCombineFNV(h, tokens[i + j]);
                int idx = (int)(h % (uint)FeatureCount);
                dst[idx] += 1f;
            }
        }

        if (_useCharNgrams)
        {
            var s = text.ToLowerInvariant();
            for (int i = 0; i < s.Length; i++)
            {
                for (int n = _ngramMin; n <= _ngramMax && i + n <= s.Length; n++)
                {
                    var h = 2166136261u;
                    for (int k = 0; k < n; k++) h = HashCombineFNV(h, s[i + k]);
                    int idx = (int)(h % (uint)FeatureCount);
                    dst[idx] += 1f;
                }
            }
        }
    }

    private static List<string> Tokenize(string text)
    {
        var list = new List<string>(32);
        var sb = new StringBuilder();
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
            else
            {
                if (sb.Length > 0) { list.Add(sb.ToString()); sb.Clear(); }
            }
        }
        if (sb.Length > 0) list.Add(sb.ToString());
        return list;
    }

    private static uint HashCombineFNV(uint h, string token)
    {
        const uint prime = 16777619u;
        foreach (var ch in token)
        {
            h ^= (byte)ch;
            h *= prime;
        }
        return h;
    }

    private static uint HashCombineFNV(uint h, char ch)
    {
        const uint prime = 16777619u;
        h ^= (byte)ch;
        h *= prime;
        return h;
    }
}