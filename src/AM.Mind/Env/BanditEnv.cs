using AM.Mind.Interfaces;
using AM.Mind.Records;
using AM.Mind.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Env;

/// <summary>
/// N-armed bandit. Observation is a constant bias vector (size >= 1).
/// Reward is drawn from N(mean[act], sigma[act]).
/// Optional drifting means: small random walk per tick.
/// </summary>
public sealed class BanditEnv : IEnvironment<VectorObs, DiscreteAct>
{
    private readonly int _k;
    private readonly int _stateDim;
    private readonly double[] _means;
    private readonly double[] _sigma;
    private readonly bool _drifting;
    private readonly double _driftStd;
    private readonly IRng _rng;

    private VectorObs _obs; // constant bias observation

    public BanditEnv(int k, int stateDim, double[] means, double[] sigma, bool drifting, double driftStd, IRng rng)
    {
        if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
        if (stateDim <= 0) throw new ArgumentOutOfRangeException(nameof(stateDim));
        if (means.Length != k || sigma.Length != k) throw new ArgumentException("means/sigma size must equal k");

        _k = k;
        _stateDim = stateDim;
        _means = (double[])means.Clone();
        _sigma = (double[])sigma.Clone();
        _drifting = drifting;
        _driftStd = driftStd;
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));

        // constant bias feature at index 0
        var v = new float[_stateDim];
        v[0] = 1f;
        _obs = new VectorObs(v);
    }

    public VectorObs Reset() => _obs;

    public EnvStepResult Step(DiscreteAct act)
    {
        int a = Math.Clamp(act.Index, 0, _k - 1);
        // Draw reward
        double r = _means[a] + _sigma[a] * _rng.NextGaussian();

        // Optional drifting of means
        if (_drifting)
        {
            for (int i = 0; i < _k; i++)
                _means[i] += _driftStd * _rng.NextGaussian();
        }

        return new EnvStepResult((float)r, _obs, Terminal: false);
    }

    public static BanditEnv Stationary(int k, int stateDim, double mean = 0.0, double spread = 1.0, double sigma = 1.0, int seed = 123)
    {
        var rng = new Rng(seed);
        var means = new double[k];
        var sig = new double[k];
        for (int i = 0; i < k; i++) { means[i] = mean + spread * rng.NextGaussian(); sig[i] = sigma; }
        return new BanditEnv(k, stateDim, means, sig, drifting: false, driftStd: 0.0, rng: rng);
    }
    public static BanditEnv Drifting(int k, int stateDim, double sigma = 1.0, double driftStd = 0.01, int seed = 123)
    {
        var rng = new Rng(seed);
        var means = new double[k];
        var sig = new double[k];
        for (int i = 0; i < k; i++) { means[i] = 0.0; sig[i] = sigma; }
        return new BanditEnv(k, stateDim, means, sig, drifting: true, driftStd: driftStd, rng: rng);
    }
}