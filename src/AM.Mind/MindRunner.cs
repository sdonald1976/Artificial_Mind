using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind;

public sealed class MindRunner : IDisposable
{
    private readonly CoreMind _mind;
    private readonly IEpisodeManager _episodes;
    private readonly IBrainClock _clock;
    private readonly Func<DiscreteAct, (float reward, VectorObs nextObs, bool terminal)> _envStep;
    private readonly int _stateDim;
    private readonly TimeSpan _period;
    private readonly CancellationTokenSource _cts = new();

    public MindRunner(
        CoreMind mind,
        IEpisodeManager episodes,
        IBrainClock clock,
        int stateDim,
        TimeSpan period,
        Func<DiscreteAct, (float reward, VectorObs nextObs, bool terminal)> envStep)
    {
        _mind = mind;
        _episodes = episodes;
        _clock = clock;
        _stateDim = stateDim;
        _period = period;
        _envStep = envStep;
    }

    public Task RunAsync()
        => _clock.RunAsync(_period, OnTickAsync, _cts.Token);

    private Task OnTickAsync(long t)
    {
        if (_episodes.Terminal || t == 0) _episodes.Begin();

        // generate a dummy observation (replace with your real input)
        Span<float> s = stackalloc float[_stateDim];
        s[0] = 1f; // bias feature
        var obs = new VectorObs(s.ToArray());

        _mind.Step(
            obs,
            episode: _episodes.CurrentEpisode,
            step: _episodes.Step,
            ticks: DateTime.UtcNow.Ticks,
            envStep: _envStep);

        _episodes.Next();
        return Task.CompletedTask;
    }

    public void Stop() => _cts.Cancel();
    public void Dispose() => _cts.Cancel();
}