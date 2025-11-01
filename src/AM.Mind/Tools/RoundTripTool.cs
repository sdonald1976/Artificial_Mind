using AM.Mind.Adapters;
using AM.Mind.IO.Models;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Tools;

public static class RoundTripTool
{
    public static void Run(string dir, string prefix, int n = 1000, int dim = 8)
    {
        Console.WriteLine("RoundTripTool: writing...");
        using var writer = new XLogWriter(dir, prefix, 8L * 1024 * 1024);
        using var sink = new XLogExperienceSink(writer, flushEvery: 32);

        var rnd = new Random(123);
        for (int i = 0; i < n; i++)
        {
            var vec = Enumerable.Range(0, dim).Select(_ => (float)rnd.NextDouble()).ToArray();
            var obs = new VectorObs(vec);
            var next = new VectorObs(vec);
            var act = new DiscreteAct(rnd.Next(3));

            sink.Append(new Experience<VectorObs, DiscreteAct>(
                Obs: obs,
                Act: act,
                Reward: 0.1f,
                NextObs: next,
                Terminal: i % 10 == 9,
                Ticks: DateTime.UtcNow.Ticks,
                Episode: 1,
                Step: i
            ));
        }

        var xlog = System.IO.Directory.GetFiles(dir, $"{prefix}-*.xlog").OrderBy(x => x).Last();
        var fidx = System.IO.Path.ChangeExtension(xlog, ".fidx");
        using var idx = new IndexReader(fidx);
        using var rdr = new XLogReader(xlog);
        Console.WriteLine($"Index entries: {idx.Count} (expected {n})");

        var last = idx.ReadAt(idx.Count - 1);
        var env = rdr.ReadAt(last.FileOffset);
        Console.WriteLine($"Last: Ep={env.Episode} Step={env.Step} Term={env.Terminal}");
    }
}