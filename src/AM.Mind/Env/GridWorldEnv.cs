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
/// GridWorld with one-hot position encoding (stateDim == width*height).
/// Actions: 0=Up, 1=Right, 2=Down, 3=Left.
/// Reward: -stepCost each move, +goalReward at goal; terminal on goal or maxSteps.
/// </summary>
public sealed class GridWorldEnv : IEnvironment<VectorObs, DiscreteAct>
{
    private readonly int _w, _h, _stateDim, _maxSteps;
    private readonly float _stepCost, _goalReward;
    private readonly (int x, int y) _goal;
    private readonly IRng _rng;

    private int _x, _y, _steps;
    private float[] _stateBuf;           // writable backing buffer
    private VectorObs _obs;              // wrapped view over _stateBuf

    public GridWorldEnv(int width, int height, (int x, int y) goal, int maxSteps, float stepCost, float goalReward, IRng rng)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException("width/height");
        if (goal.x < 0 || goal.x >= width || goal.y < 0 || goal.y >= height) throw new ArgumentException("goal out of bounds");
        if (maxSteps <= 0) throw new ArgumentOutOfRangeException(nameof(maxSteps));

        _w = width; _h = height; _stateDim = width * height;
        _goal = goal; _maxSteps = maxSteps;
        _stepCost = stepCost; _goalReward = goalReward;
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));

        _stateBuf = new float[_stateDim];
        _obs = new VectorObs(_stateBuf);
    }

    public VectorObs Reset()
    {
        // random start (not on goal)
        do { _x = _rng.NextInt(_w); _y = _rng.NextInt(_h); } while (_x == _goal.x && _y == _goal.y);
        _steps = 0;
        UpdateObs();
        return _obs;
    }

    public EnvStepResult Step(DiscreteAct act)
    {
        int a = act.Index;
        int nx = _x, ny = _y;
        switch (a)
        {
            case 0: ny = Math.Max(0, _y - 1); break;        // Up
            case 1: nx = Math.Min(_w - 1, _x + 1); break;   // Right
            case 2: ny = Math.Min(_h - 1, _y + 1); break;   // Down
            case 3: nx = Math.Max(0, _x - 1); break;        // Left
            default: break;                                  // invalid -> no move
        }
        _x = nx; _y = ny;
        _steps++;

        bool atGoal = (_x == _goal.x && _y == _goal.y);
        float r = -_stepCost + (atGoal ? _goalReward : 0f);
        bool terminal = atGoal || _steps >= _maxSteps;

        UpdateObs();
        return new EnvStepResult(r, _obs, terminal);
    }

    private void UpdateObs()
    {
        Array.Clear(_stateBuf, 0, _stateDim);
        int idx = _y * _w + _x;
        _stateBuf[idx] = 1f;
        // _obs already wraps _stateBuf; no need to reassign unless you reallocate the buffer
    }

    public static GridWorldEnv Default5x5((int x, int y)? goal = null, int maxSteps = 100, float stepCost = 0.01f, float goalReward = 1f, int seed = 123)
    {
        var rng = new Rng(seed);
        var g = goal ?? (4, 4);
        return new GridWorldEnv(5, 5, g, maxSteps, stepCost, goalReward, rng);
    }
}