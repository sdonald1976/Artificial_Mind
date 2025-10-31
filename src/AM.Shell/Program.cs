using AM.Mind;
using AM.Mind.IO;
using AM.Mind.IO.Models;

namespace AM.Shell
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //int stateDim = 4;
            //int actionDim = 3;

            ////var env = new ToyEnv(stateDim);
            //var brain = new CoreMind(stateDim, actionDim, learningRate: 0.05f);

            //for (int episode = 0; episode < 200; episode++)
            //{
            //if (env.IsDone) break;
            //var s = env.GetState();
            //int a = brain.Act(s);
            //float r = env.Step(a);
            //brain.Update(s, a, r);

            //if ((episode + 1) % 20 == 0)
            //    Console.WriteLine($"Episode {episode + 1}: action={a}, reward={r}");
            //}

            //Console.WriteLine("Done.");

            var dir = "data";
            using (var writer = new XLogWriter(dir, prefix: "exp", rotateBytes: 512L * 1024 * 1024))
            {
                var rng = new Random(0);
                for (int i = 0; i < 5; i++)
                {
                    Span<float> s = stackalloc float[4];
                    for (int j = 0; j < s.Length; j++) s[j] = (float)rng.NextDouble();

                    var env = ExperienceEnvelope.VectorObsDiscreteAct(
                        id: 0, // writer assigns
                        ticks: DateTime.UtcNow.Ticks,
                        episode: 1, step: i,
                        reward: (float)Math.Round(rng.NextSingle() * 2 - 1, 3),
                        terminal: i == 4,
                        obsVector: s,
                        actionIndex: rng.Next(0, 3)
                    );

                    long id = writer.Append(env);
                    Console.WriteLine($"Appended Id={id}");
                }
                writer.Flush();
            }

            // Read back using index
            var xlogPath = System.IO.Directory.GetFiles("data", "exp-*.xlog")[0];
            var fidxPath = System.IO.Path.ChangeExtension(xlogPath, ".fidx");

            using var idx = new IndexReader(fidxPath);
            using var rdr = new XLogReader(xlogPath);

            Console.WriteLine($"Index count = {idx.Count}");
            foreach (var e in idx.All())
            {
                var env = rdr.ReadAt(e.FileOffset);
                Console.WriteLine($"Id={env.Id} Ep={env.Episode} Step={env.Step} Reward={env.Reward} Term={env.Terminal}");
            }

            // Or sequentially:
            // foreach (var (off, env) in rdr.ReadAll()) { ... }


        }
    }
}
