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
        static void Main(string[] args)
        {
            // Wiring: RAM-only sink/recall; vector policy; pass-through reward.
            var recall = new RingRecall<VectorObs, DiscreteAct>(capacity: 32);
            var policy = new LinearSoftmaxPolicy(stateDim: 4, actionDim: 3, learningRate: 0.05f, seed: 42);
            var reward = new PassThroughReward<VectorObs, DiscreteAct>();
            IExperienceSink<VectorObs, DiscreteAct> sink = recall; // RAM-only

            var mind = new CoreMind(policy, sink, recall, reward);
            var rng = new Random(0);
            var obs = new VectorObs(new float[] { 0.1f, -0.2f, 0.3f, -0.4f });

            for (int step = 0; step < 10; step++)
            {
                var act = mind.Step(
                    obs,
                    episode: 1,
                    step: step,
                    ticks: DateTime.UtcNow.Ticks,
                    envStep: a =>
                    {
                        // toy environment: reward +1 if action==0, else 0
                        float r = (a.Index == 0) ? 1f : 0f;
                        return (r, obs, terminal: step == 9);
                    });

                Console.WriteLine($"t={step} act={act.Index}");
            }

            // Export/import snapshot demo:
            var snapStore = new JsonSnapshotStore();
            var snap = policy.Export();
            snapStore.Save("policy.json", snap);
            Console.WriteLine("Saved snapshot to policy.json");
        }
    }
}
