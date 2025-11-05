using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.POS;

public sealed class SoftmaxClassifier
{
    private readonly int _featDim, _numClasses;
    private readonly float[] _W; // row-major [numClasses x featDim]
    private readonly float[] _b; // [numClasses]
    private readonly float[] _logits;
    private readonly float[] _probs;
    private readonly Random _rng;

    public int FeatureDim => _featDim;
    public int NumClasses => _numClasses;

    public SoftmaxClassifier(int featDim, int numClasses, int seed = 123)
    {
        _featDim = featDim; _numClasses = numClasses;
        _W = new float[numClasses * featDim];
        _b = new float[numClasses];
        _logits = new float[numClasses];
        _probs = new float[numClasses];
        _rng = new Random(seed);

        // small init
        float scale = 1f / (float)Math.Sqrt(featDim);
        for (int i = 0; i < _W.Length; i++)
            _W[i] = (float)((_rng.NextDouble() * 2 - 1) * 0.01 * scale);
    }

    public int Predict(ReadOnlySpan<float> x)
    {
        ComputeLogits(x, _logits);
        SoftmaxInPlace(_logits, _probs);
        return ArgMax(_probs);
    }

    // One SGD step (x, y), returns loss
    public float TrainStep(ReadOnlySpan<float> x, int y, float lr = 0.05f)
    {
        ComputeLogits(x, _logits);
        SoftmaxInPlace(_logits, _probs);

        float loss = -MathF.Log(MathF.Max(1e-8f, _probs[y]));

        // gradient: probs - onehot(y)
        _probs[y] -= 1f;

        // update
        for (int c = 0; c < _numClasses; c++)
        {
            int row = c * _featDim;
            float gc = _probs[c];
            _b[c] -= lr * gc;
            for (int j = 0; j < _featDim; j++)
                _W[row + j] -= lr * gc * x[j];
        }

        return loss;
    }

    private void ComputeLogits(ReadOnlySpan<float> x, float[] logits)
    {
        for (int c = 0; c < _numClasses; c++)
        {
            int row = c * _featDim;
            float s = _b[c];
            for (int j = 0; j < _featDim; j++)
                s += _W[row + j] * x[j];
            logits[c] = s;
        }
    }

    private static void SoftmaxInPlace(float[] logits, float[] probs)
    {
        float max = logits[0];
        for (int i = 1; i < logits.Length; i++) if (logits[i] > max) max = logits[i];
        double sum = 0;
        for (int i = 0; i < logits.Length; i++) { double e = Math.Exp(logits[i] - max); probs[i] = (float)e; sum += e; }
        float inv = (float)(1.0 / sum);
        for (int i = 0; i < probs.Length; i++) probs[i] *= inv;
    }

    private static int ArgMax(float[] v)
    {
        int idx = 0; float best = v[0];
        for (int i = 1; i < v.Length; i++) if (v[i] > best) { best = v[i]; idx = i; }
        return idx;
    }
}
