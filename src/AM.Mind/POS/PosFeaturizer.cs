using AM.Mind.Interfaces;
using AM.Mind.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.POS;

public sealed class PosFeaturizer
{
    private readonly int _window;          // tokens left/right
    private readonly int _featPerSlot;     // hashed size per token slot
    private readonly ITextEncoder _hasher; // we’ll use it as a bag-of-ngrams hasher

    //public int FeatureDim => (2 * _window + 1) * _featPerSlot;
    public int FeatureDim => (2 * _window + 1) * _featPerSlot + 1;

    public PosFeaturizer(int window = 2, int featPerSlot = 512, ITextEncoder? hasher = null)
    {
        _window = Math.Max(0, window);
        _featPerSlot = Math.Max(32, featPerSlot);
        _hasher = hasher ?? new HashedTextEncoder(featureCount: featPerSlot, ngramMin: 1, ngramMax: 2, useCharNgrams: false);
    }

    // Build a sparse-ish dense vector by placing each slot’s hashed vector into its segment.
    public void Encode(string[] tokens, int i, float[] dst)
    {
        Array.Clear(dst, 0, dst.Length);
        int center = _window;

        for (int offset = -_window; offset <= _window; offset++)
        {
            int slot = center + offset;
            int start = slot * _featPerSlot;

            string tok = GetToken(tokens, i + offset);
            _hasher.Encode(tok, dst.AsSpan(start, _featPerSlot).ToArray()); // encode into a temp then add
            // Above created a temp; improve by writing directly:
            var tmp = new float[_featPerSlot];
            _hasher.Encode(tok, tmp);
            for (int k = 0; k < _featPerSlot; k++) dst[start + k] += tmp[k];
        }
        // bias term:
        dst[^1] = 1f;
    }

    private static string GetToken(string[] arr, int idx)
        => (idx < 0 || idx >= arr.Length) ? "<PAD>" : arr[idx];
}