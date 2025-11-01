using AM.Mind;
using AM.Mind.Adapters;
using AM.Mind.Config;
using AM.Mind.Diagnostics;
using AM.Mind.Env;
using AM.Mind.Eval;
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
            var cfg = new AgentConfig
            {
                StateDim = 4,
                ActionDim = 3,
                Seed = 42,
                FlushEvery = 64,
                RotateBytes = 256L * 1024 * 1024,
                BufferedQueueCapacity = 4096,
                DropWhenFull = false,
                DataDir = "data",
                Prefix = "exp"
            };

            var recall = new RingRecall<VectorObs, DiscreteAct>(capacity: 512);
            var policy = new LinearSoftmaxPolicy(cfg.StateDim, cfg.ActionDim, learningRate: 0.05f, seed: cfg.Seed);

            // Example reward shaping (optional): encourage action 0, discourage repeats
            var reward = new AM.Mind.Rewards.CompositeReward()
                .Add(new AM.Mind.Rewards.TargetActionReward(0, 0.25f))
                .Add(new AM.Mind.Rewards.RepeatPenaltyReward(3, 0.10f));

            // Durable sink (wrapped in buffered sink for backpressure)
            var writer = new AM.Mind.IO.Models.XLogWriter(cfg.DataDir, cfg.Prefix, cfg.RotateBytes);
            var disk = new AM.Mind.Adapters.XLogExperienceSink(writer, cfg.FlushEvery);
            var sink = new AM.Mind.Adapters.BufferedExperienceSink<VectorObs, DiscreteAct>(disk, cfg.BufferedQueueCapacity, cfg.DropWhenFull);
            AM.Mind.Util.ProcessExitFlusher.Register(sink);

            var mind = new CoreMind(policy, sink, recall, reward);
            var episodes = new EpisodeManager();
            var clock = new BrainClock();
            var telem = new StepTelemetry();

            // Toy environment: reward +1 if action==0
            (float, VectorObs, bool) Env(DiscreteAct a)
                => (a.Index == 0 ? 1f : 0f, new VectorObs(ReadOnlyMemory<float>.Empty), false);

            var bandit = BanditEnv.Stationary(cfg.ActionDim, cfg.StateDim, mean: 0.2, spread: 0.6, sigma: 0.5, seed: cfg.Seed);
            VectorObs initial = bandit.Reset();

            // Reward shaping: favor best arm (unknown to policy; we define as 0 for test)
            var shaped = new AM.Mind.Rewards.CompositeReward()
                .Add(new AM.Mind.Rewards.GoalReward(e => e.Act.Index == 0, bonus: 0.25f))
                .Add(new AM.Mind.Rewards.RepeatPenaltyReward(3, 0.05f));
            // swap reward system
            var coreMind = new CoreMind(policy, sink, recall, shaped);
            // Run a few episodes synchronously
            var epStats = EvalRunner.RunEpisodes(coreMind, bandit, episodes: 50, maxStepsPerEpisode: 100);
            Console.WriteLine($"Ran {epStats.Count} episodes; last totalR={epStats[^1].TotalReward:F3}");

            //var runner = new MindRunner(mind, episodes, clock, cfg.StateDim, TimeSpan.FromMilliseconds(10), a => Env(a));
            var runner = new MindRunner(mind, episodes, clock, cfg.StateDim, TimeSpan.FromMilliseconds(10), EnvAdapters.AsFunc(bandit));

            //OR
            //var grid = GridWorldEnv.Default5x5(goal: (4,4), maxSteps: 100, stepCost: 0.01f, goalReward: 1f, seed: cfg.Seed);
            //VectorObs initial = grid.Reset();
            //var runner = new MindRunner(mind, episodes, clock, cfg.StateDim, TimeSpan.FromMilliseconds(30), AM.Mind.Envs.EnvAdapters.AsFunc(grid));

            Console.CancelKeyPress += (_, __) => runner.Stop();

            // Lightweight stats task (prints once per second)
            using var statsCts = new CancellationTokenSource();
            var statsTask = Task.Run(async () =>
            {
                var obsProbe = new VectorObs(new float[] { 1, 0, 0, 0 });
                while (!statsCts.IsCancellationRequested)
                {
                    var p = policy.ProbsFor(obsProbe);
                    Console.WriteLine($"π=[{string.Join(", ", Array.ConvertAll(p, x => x.ToString("F2")))}]  drops={sink.Dropped} wrote={sink.Written}");
                    await Task.Delay(1000, statsCts.Token);
                }
            }, statsCts.Token);

            Console.WriteLine("Running. Press Ctrl+C to stop...");
            await runner.RunAsync();

            statsCts.Cancel();
            try { await statsTask; } catch { /* ignore */ }
        }
    }
}
