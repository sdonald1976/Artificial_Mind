using AM.Mind;
using AM.Mind.Adapters;
using AM.Mind.Interfaces;
using AM.Mind.IO;
using AM.Mind.IO.Models;
using AM.Mind.Models;
using AM.Mind.Policies;
using AM.Mind.Records;

namespace AM.Shell
{
    internal class Program
    {
        static async Task Main()
        {
            bool useDisk = true;             // flip to false for RAM-only
            int stateDim = 4, actionDim = 3;

            var recall = new RingRecall<VectorObs, DiscreteAct>(capacity: 128);
            var policy = new LinearSoftmaxPolicy(stateDim, actionDim, learningRate: 0.05f, seed: 42);
            var reward = new PassThroughReward<VectorObs, DiscreteAct>();

            IExperienceSink<VectorObs, DiscreteAct> sink =
                useDisk
                ? new AM.Mind.Adapters.XLogExperienceSink(new AM.Mind.IO.Models.XLogWriter("data", "exp", 256L * 1024 * 1024))
                : recall; // RAM-only

            var mind = new CoreMind(policy, sink, recall, reward);
            var episodes = new EpisodeManager();
            var clock = new BrainClock();

            // toy environment: +1 if act==0, else 0; never terminates here
            (float, VectorObs, bool) Env(DiscreteAct a)
                => (a.Index == 0 ? 1f : 0f, new VectorObs(ReadOnlyMemory<float>.Empty), false);

            var runner = new MindRunner(mind, episodes, clock, stateDim, TimeSpan.FromMilliseconds(50), a => Env(a));
            Console.CancelKeyPress += (_, __) => runner.Stop();

            Console.WriteLine("Running. Press Ctrl+C to stop...");
            await runner.RunAsync();
        }
    }
}
