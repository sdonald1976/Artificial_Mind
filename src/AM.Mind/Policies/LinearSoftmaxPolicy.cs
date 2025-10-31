using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Policies;

public sealed class LinearSoftmaxPolicy : IPolicy<VectorObs, DiscreteAct>
{
    private readonly int _stateDim, _actionDim;
    private readonly float[] _W;    // [actionDim * stateDim]
    private readonly float[] _b;    // [actionDim]
    private readonly float _lr;
    private readonly Random _rng = new(1234);

    // scratch
    private readonly float[] _logits;
    private readonly float[] _probs;

    public LinearSoftmaxPolicy(int stateDim, int actionDim, float learningRate = 1e-2f)
    {
        if (stateDim <= 0) throw new ArgumentOutOfRangeException(nameof(stateDim));
        if (actionDim <= 0) throw new ArgumentOutOfRangeException(nameof(actionDim));

        _stateDim = stateDim;
        _actionDim = actionDim;
        _lr = learningRate;

        _W = new float[actionDim * stateDim];
        _b = new float[actionDim];
        _logits = new float[actionDim];
        _probs = new float[actionDim];

        float scale = 1f / (float)Math.Sqrt(stateDim);
        for (int i = 0; i < _W.Length; i++) _W[i] = (float)(NextGaussian() * scale * 0.1);
    }

    public DiscreteAct Decide(in VectorObs obs, ReadOnlySpan<VectorObs> recent)
    {
        var x = obs.Features.Span;
        ComputeLogits(x, _logits);
        SoftmaxInPlace(_logits, _probs);
        return new DiscreteAct(Sample(_probs));
    }

    public void Update(in VectorObs obs, in DiscreteAct act, float reward)
    {
        var x = obs.Features.Span;
        ComputeLogits(x, _logits);
        SoftmaxInPlace(_logits, _probs);

        int aIdx = act.Index;
        _probs[aIdx] -= 1f; // d(-log pi)/dlogits = probs; probs[a]-=1

        float scale = -_lr * reward; // ascent on reward
        for (int a = 0; a < _actionDim; a++)
        {
            float gBias = _probs[a];
            _b[a] += scale * gBias;

            int row = a * _stateDim;
            for (int j = 0; j < _stateDim; j++)
                _W[row + j] += scale * (gBias * x[j]);
        }
    }

    private void ComputeLogits(ReadOnlySpan<float> x, float[] logits)
    {
        for (int a = 0; a < _actionDim; a++)
        {
            int row = a * _stateDim;
            float sum = _b[a];
            for (int j = 0; j < _stateDim; j++) sum += _W[row + j] * x[j];
            logits[a] = sum;
        }
    }

    private static void SoftmaxInPlace(float[] logits, float[] probs)
    {
        float max = logits[0];
        for (int i = 1; i < logits.Length; i++) if (logits[i] > max) max = logits[i];
        double s = 0;
        for (int i = 0; i < logits.Length; i++) { double e = Math.Exp(logits[i] - max); probs[i] = (float)e; s += e; }
        float inv = (float)(1.0 / s);
        for (int i = 0; i < probs.Length; i++) probs[i] *= inv;
    }

    private int Sample(float[] probs)
    {
        double r = _rng.NextDouble(), c = 0;
        for (int i = 0; i < probs.Length; i++) { c += probs[i]; if (r <= c) return i; }
        return probs.Length - 1;
    }

    private double NextGaussian()
    {
        double u1 = 1.0 - _rng.NextDouble(), u2 = 1.0 - _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}