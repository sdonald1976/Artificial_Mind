using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Policies;

/// <summary>
/// Wraps a discrete policy to produce TextAct from a fixed template list.
/// Uses the same VectorObs input.
/// </summary>
public sealed class TemplatePolicy : IPolicy<VectorObs, TextAct>
{
    private readonly IPolicy<VectorObs, DiscreteAct> _inner;
    private readonly string[] _templates;

    public TemplatePolicy(IPolicy<VectorObs, DiscreteAct> inner, params string[] templates)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _templates = templates is { Length: > 0 } ? templates : throw new ArgumentException("Need at least one template");
    }

    public TextAct Decide(in VectorObs obs, ReadOnlySpan<VectorObs> recent)
    {
        var a = _inner.Decide(obs, recent);
        int idx = Math.Clamp(a.Index, 0, _templates.Length - 1);
        return new TextAct(_templates[idx]);
    }

    public void Update(in VectorObs obs, in TextAct act, float reward)
    {
        // Map chosen template back to its index; fall back to 0 if not found.
        int idx = Array.IndexOf(_templates, act.Text);
        if (idx < 0) idx = 0;
        _inner.Update(obs, new DiscreteAct(idx), reward);
    }
}