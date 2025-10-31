using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Diagnostics;

public sealed class StepTelemetry
{
    public long Steps { get; private set; }
    public double RewardSum { get; private set; }
    public double RewardMean => Steps == 0 ? 0 : RewardSum / Steps;
    public double LastReward { get; private set; }
    public TimeSpan LastStepTime { get; private set; }

    private DateTime _last = DateTime.UtcNow;

    public void Record(float reward)
    {
        Steps++;
        RewardSum += reward;
        LastReward = reward;
        var now = DateTime.UtcNow;
        LastStepTime = now - _last;
        _last = now;
    }

    public override string ToString()
        => $"steps={Steps} reward_mean={RewardMean:F3} last_r={LastReward:F3} dt={LastStepTime.TotalMilliseconds:F1}ms";
}