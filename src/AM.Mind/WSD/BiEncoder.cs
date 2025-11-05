using AM.Mind.Interfaces;
using AM.Mind.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AM.Mind.WSD;

public sealed class BiEncoderWsd
{
    private readonly ITextEncoder _ctxHasher;
    private readonly ITextEncoder _glsHasher;
    private readonly int _featCtx, _featGls, _dim;
    private readonly float[] _Wc;  // [featCtx x dim] row-major
    private readonly float[] _Wg;  // [featGls x dim] row-major
    private readonly float[] _xc;  // ctx feature buffer
    private readonly float[] _xg;  // gloss feature buffer
    private readonly float[] _yc;  // projected ctx
    private readonly float[] _yg;  // projected gloss
    private float[] _scores; // candidate scores softmax buffer
    private readonly Random _rng = new(123);

    public int ContextFeatureDim => _featCtx;
    public int GlossFeatureDim => _featGls;
    public int ProjectionDim => _dim;

    public BiEncoderWsd(int ctxFeatures = 4096, int glsFeatures = 4096, int projDim = 128, int seed = 123)
    {
        _featCtx = ctxFeatures;
        _featGls = glsFeatures;
        _dim = projDim;

        _ctxHasher = new HashedTextEncoder(featureCount: ctxFeatures, ngramMin: 1, ngramMax: 2, useCharNgrams: false);
        _glsHasher = new HashedTextEncoder(featureCount: glsFeatures, ngramMin: 1, ngramMax: 2, useCharNgrams: false);

        _Wc = new float[_featCtx * _dim];
        _Wg = new float[_featGls * _dim];
        _xc = new float[_featCtx];
        _xg = new float[_featGls];
        _yc = new float[_dim];
        _yg = new float[_dim];
        _scores = Array.Empty<float>();

        Init(_Wc, _rng);
        Init(_Wg, _rng);
    }
    private void EnsureScoresCapacity(int n)
    {
        if (_scores.Length < n) Array.Resize(ref _scores, n);
    }
    private static void Init(float[] w, Random rng)
    {
        double s = 0.02;
        for (int i = 0; i < w.Length; i++)
            w[i] = (float)((rng.NextDouble() * 2 - 1) * s);
    }

    // ---- Encoding helpers ----
    public void EncodeContext(string[] tokens, int targetIndex, int window = 5)
    {
        Array.Clear(_xc, 0, _xc.Length);
        int L = tokens.Length;
        int left = Math.Max(0, targetIndex - window);
        int right = Math.Min(L - 1, targetIndex + window);
        for (int i = left; i <= right; i++)
        {
            _ctxHasher.Encode(tokens[i], _xc); // bag add
        }
    }

    public void EncodeGloss(string gloss)
    {
        Array.Clear(_xg, 0, _xg.Length);
        _glsHasher.Encode(gloss, _xg);
    }

    public void ProjectContext() => MatVec(_Wc, _featCtx, _dim, _xc, _yc);
    public void ProjectGloss() => MatVec(_Wg, _featGls, _dim, _xg, _yg);

    private static void MatVec(float[] W, int rows, int cols, float[] x, float[] y)
    {
        for (int c = 0; c < cols; c++)
        {
            double s = 0;
            int baseIdx = c; // column c is stride=cols? We stored row-major [rows x cols]:
                             // Compute y[c] = sum_r W[r*cols + c] * x[r]
            for (int r = 0; r < rows; r++)
                s += W[r * cols + c] * x[r];
            y[c] = (float)s;
        }
    }

    private static float Dot(float[] a, float[] b)
    {
        double s = 0;
        for (int i = 0; i < a.Length; i++) s += a[i] * b[i];
        return (float)s;
    }

    // ---- Inference ----
    public int Predict(string[] tokens, int targetIndex, ReadOnlySpan<string> glosses)
    {
        EnsureScoresCapacity(glosses.Length);
        EncodeContext(tokens, targetIndex);
        ProjectContext();

        for (int i = 0; i < glosses.Length; i++)
        {
            EncodeGloss(glosses[i]);
            ProjectGloss();
            _scores[i] = Dot(_yc, _yg);
        }
        return ArgMax(_scores, glosses.Length);
    }

    // ---- Training ----
    /// <summary>
    /// One supervised step. Returns loss.
    /// </summary>
    public float TrainStep(string[] tokens, int targetIndex, string[] glosses, int goldIndex, float lr = 0.05f)
    {
        int K = glosses.Length;
        EnsureScoresCapacity(K);

        // Forward
        EncodeContext(tokens, targetIndex);
        ProjectContext();

        // For each gloss, compute projection and score; also keep yg vectors (need them for grads)
        // We'll store them consecutively to avoid allocations.
        float[] ygAll = new float[K * _dim];
        for (int i = 0; i < K; i++)
        {
            EncodeGloss(glosses[i]);
            ProjectGloss();
            // copy _yg
            Array.Copy(_yg, 0, ygAll, i * _dim, _dim);
            _scores[i] = Dot(_yc, _yg);
        }

        // Softmax
        float max = _scores[0];
        for (int i = 1; i < K; i++) if (_scores[i] > max) max = _scores[i];
        double sum = 0;
        for (int i = 0; i < K; i++) { double e = Math.Exp(_scores[i] - max); _scores[i] = (float)e; sum += e; }
        float inv = (float)(1.0 / sum);
        for (int i = 0; i < K; i++) _scores[i] *= inv; // now probs

        float loss = -MathF.Log(MathF.Max(1e-8f, _scores[goldIndex]));

        // Gradients:
        // dL/ds_i = p_i - 1_{i=gold}
        // dL/dy_c = sum_i (p_i - 1_{i=gold}) * y_g[i]
        // dL/dy_g[i] = (p_i - 1_{i=gold}) * y_c
        var dYc = new float[_dim];
        for (int i = 0; i < K; i++)
        {
            float gi = _scores[i] - (i == goldIndex ? 1f : 0f);
            int baseIdx = i * _dim;
            for (int d = 0; d < _dim; d++)
                dYc[d] += gi * ygAll[baseIdx + d];
        }

        // Update Wc using x_c and dYc: Wc[r,c] -= lr * x_c[r] * dYc[c]
        for (int r = 0; r < _featCtx; r++)
        {
            float xr = _xc[r];
            if (xr == 0f) continue;
            int rowBase = r * _dim;
            for (int d = 0; d < _dim; d++)
                _Wc[rowBase + d] -= lr * xr * dYc[d];
        }

        // Update Wg for each gloss i using dYg_i = gi * y_c
        for (int i = 0; i < K; i++)
        {
            float gi = _scores[i] - (i == goldIndex ? 1f : 0f);
            if (gi == 0f) continue;

            // Recompute gloss features for i (we need _xg filled to update Wg).
            EncodeGloss(glosses[i]);

            for (int r = 0; r < _featGls; r++)
            {
                float xr = _xg[r];
                if (xr == 0f) continue;
                int rowBase = r * _dim;
                for (int d = 0; d < _dim; d++)
                    _Wg[rowBase + d] -= lr * xr * (gi * _yc[d]);
            }
        }

        return loss;
    }

    private static int ArgMax(float[] v, int n)
    {
        int idx = 0; float best = v[0];
        for (int i = 1; i < n; i++) if (v[i] > best) { best = v[i]; idx = i; }
        return idx;
    }

    // === NEW: serialization DTO ===
    private sealed class Serializable
    {
        public int CtxFeatures { get; set; }
        public int GlsFeatures { get; set; }
        public int Dim { get; set; }
        public float[] Wc { get; set; } = Array.Empty<float>();
        public float[] Wg { get; set; } = Array.Empty<float>();
        public int Seed { get; set; }
    }

    public string ToJson(bool indented = true)
    {
        var dto = new Serializable
        {
            CtxFeatures = _featCtx,
            GlsFeatures = _featGls,
            Dim = _dim,
            Wc = _Wc,
            Wg = _Wg,
            Seed = 123
        };
        return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = indented });
    }

    public static BiEncoderWsd FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<Serializable>(json)!;
        var m = new BiEncoderWsd(dto.CtxFeatures, dto.GlsFeatures, dto.Dim, seed: dto.Seed);
        Array.Copy(dto.Wc, m._Wc, dto.Wc.Length);
        Array.Copy(dto.Wg, m._Wg, dto.Wg.Length);
        return m;
    }
}