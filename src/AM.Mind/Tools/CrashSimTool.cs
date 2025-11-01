using AM.Mind.Adapters;
using AM.Mind.IO.Models;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Tools;

public static class CrashSimTool
{
    public static void Run(string dir, string prefix, int total = 10000, int killAfter = 2500)
    {
        Console.WriteLine("CrashSimTool: writing...");
        var writer = new XLogWriter(dir, prefix, 8L * 1024 * 1024);
        var sink = new XLogExperienceSink(writer, flushEvery: 128);

        for (int i = 0; i < total; i++)
        {
            var obs = new VectorObs(new float[] { 1, 2, 3, 4 });
            sink.Append(new Experience<VectorObs, DiscreteAct>(obs, new DiscreteAct(i % 3), 0f, obs, false, DateTime.UtcNow.Ticks, 1, i));
            if (i == killAfter)
            {
                Console.WriteLine("Simulating crash now...");
                Environment.FailFast("Simulated crash"); // abrupt process death
            }
        }
    }
}