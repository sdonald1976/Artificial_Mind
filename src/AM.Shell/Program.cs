using AM.Mind;
using AM.Mind.Adapters;
using AM.Mind.Config;
using AM.Mind.Diagnostics;
using AM.Mind.Env;
using AM.Mind.Eval;
using AM.Mind.Interfaces;
using AM.Mind.IO;
using AM.Mind.IO.Models;
using AM.Mind.LM;
using AM.Mind.Models;
using AM.Mind.Policies;
using AM.Mind.Records;
using AM.Mind.Text;
using AM.Mind.Tools;
using AM.Mind.Util;
using AM.Mind.WSD;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AM.Shell
{
    internal class Program
    {
        private sealed class WsdTrainState
        {
            public int EpochCompleted { get; set; } // number of completed epochs
        }
        static async Task Main(string[] args)
        {
            //var conn = "Server=127.0.0.1;Database=OEWN;TrustServerCertificate=True;User ID=sa;Password=R409oo76!;";
            //Directory.CreateDirectory("data/lm");
            //Directory.CreateDirectory("models");

            //// 1) Export LM lines
            //var lmSql = @"SELECT definition FROM dbo.synsets WHERE definition IS NOT NULL UNION ALL SELECT sample     FROM dbo.samples  WHERE sample     IS NOT NULL;";
            //using (var sw = new StreamWriter("data/lm/text.txt"))
            //using (var cn = new SqlConnection(conn))
            //using (var cmd = new SqlCommand(lmSql, cn))
            //{
            //    cn.Open();
            //    using var rd = cmd.ExecuteReader();
            //    while (rd.Read())
            //    {
            //        var line = rd.GetString(0).Trim();
            //        if (line.Length > 0) sw.WriteLine(line);
            //    }
            //}

            //// 2) Train tokenizer on exported text
            //var lines = File.ReadAllLines("data/lm/text.txt");
            //var bpeModel = BpeTokenizer.Train(lines, vocabSize: 8192, minCount: 2);
            //File.WriteAllText("models/tokenizer.json", bpeModel.ToJson(true));
            //var tok = new BpeTokenizer(bpeModel);

            //// 3) Train an n-gram LM and evaluate perplexity
            //var seqs = NGramIO.EncodeLines(tok, lines);
            //var lm = new NGramLm(order: 3, bosId: tok.BosId, eosId: tok.EosId, discount: 0.75f);
            //lm.Fit(seqs);
            //Console.WriteLine($"NGram PPL: {lm.Perplexity(seqs):F2}");
            //NGramIO.Save(lm, "models/ngram-kn.json");

            //// 4) Sample
            //var sample = LmSampler.Generate(tok, lm, prompt: "the", maxNewTokens: 32, seed: 42);
            //Console.WriteLine(sample);

            //// 5) Training
            //// Wire it together
            //var inv = new SqlServerSenseInventory(conn);
            //var wsd = new BiEncoderWsd(ctxFeatures: 4096, glsFeatures: 4096, projDim: 128, seed: 42);

            //// build dataset
            //var all = WsdDatasetBuilder.BuildWsdDataset(conn);
            //var split = (int)(all.Count * 0.9);
            //var train = all.GetRange(0, split);
            //var dev = all.GetRange(split, all.Count - split);

            //// train epochs
            //for (int ep = 1; ep <= 5; ep++)
            //{
            //    var (loss, acc) = WsdTrainer.TrainEpoch(wsd, train, inv, lr: 0.05f, maxCandidates: 16);
            //    var devAcc = WsdTrainer.Evaluate(wsd, dev, inv, maxCandidates: 16);
            //    Console.WriteLine($"WSD ep{ep} loss={loss:F4} trainAcc={acc:P1} devAcc={devAcc:P1}");
            //}

            // Load your tokenizer and the same text you trained on
            //var tokPath = "models/tokenizer.json";
            //var lines = System.IO.File.ReadAllLines("data/lm/text.txt");

            //// Produce reports to ./reports/tok1
            //TokenizerInspector.Analyze(tokPath, lines, outDir: "reports/tok1", topN: 200, sampleWords: 200);

            //Console.WriteLine("Wrote reports in reports/tok1:\n  - summary.txt\n  - top_pieces.csv\n  - rare_pieces.csv\n  - word_segments.csv");

            var cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

            // --- EDIT THESE TO MATCH YOUR MACHINE ---
            var cfg = new Config
            {
                Conn = "Server=127.0.0.1;Database=OEWN;TrustServerCertificate=True;User ID=sa;Password=R409oo76!;",
                DataDir = "data",
                ModelsDir = "models",
                ReportsDir = "reports",
                // tokenizer
                VocabSize = 16384, //8192
                MinCount = 2,
                // LM
                NGramOrder = 3,
                // WSD
                CtxFeat = 4096,
                GlsFeat = 4096,
                ProjDim = 128,
                Lr = 0.05f,
                MaxCandidates = 16,
                Epochs = 3,
                MaxPerLemmaPos = 2000
            };

            Directory.CreateDirectory(cfg.DataDir);
            Directory.CreateDirectory(cfg.ModelsDir);
            Directory.CreateDirectory(cfg.ReportsDir);

            try
            {
                switch (cmd)
                {
                    case "all":
                        LmExportAndTrain(cfg);
                        TokReport(cfg);
                        WsdBuildAndTrain(cfg);
                        break;

                    case "lm":
                        LmExportAndTrain(cfg);
                        break;

                    case "tok-report":
                        TokReport(cfg);
                        break;

                    case "wsd":
                        WsdBuildAndTrain(cfg);
                        break;

                    default:
                        Console.WriteLine("Commands: all | lm | tok-report | wsd");
                        return;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FATAL: " + ex);
                return;
            }

            return;
        }

        // -------------------------
        // CONFIG
        // -------------------------
        public class Config
        {
            public string Conn = "";
            public string DataDir = "data";
            public string ModelsDir = "models";
            public string ReportsDir = "reports";

            public int VocabSize = 8192;
            public int MinCount = 2;

            public int NGramOrder = 5;

            public int CtxFeat = 4096;
            public int GlsFeat = 4096;
            public int ProjDim = 128;
            public float Lr = 0.05f;
            public int MaxCandidates = 16;
            public int Epochs = 3;
            public int MaxPerLemmaPos = 2000;
        }

        // -------------------------
        // PIPELINE: LM
        // -------------------------
        private static void LmExportAndTrain(Config cfg)
        {
            Console.WriteLine("== LM: export + tokenizer + n-gram ==");

            var lmDir = Path.Combine(cfg.DataDir, "lm");
            Directory.CreateDirectory(lmDir);

            var textPath = Path.Combine(lmDir, "text.txt");
            ExportLmLines(cfg.Conn, textPath);
            Console.WriteLine($"Exported LM lines → {textPath}");

            // Train tokenizer
            //var bpe = BpeTokenizer.Train(lines, vocabSize: cfg.VocabSize, minCount: cfg.MinCount, log: s => Console.WriteLine(s));
            var tokPath = Path.Combine(cfg.ModelsDir, "tokenizer.json");

            int lastSavedMerge = 0;

            var rawPath  = Path.Combine(cfg.DataDir, "lm", "text.txt");
            var normPath = Path.Combine(cfg.DataDir, "lm", "text.norm.txt");
            var sampPath = Path.Combine(cfg.DataDir, "lm", "text.sample.txt");

            EnsureNormalizedCorpus(rawPath, normPath);
            MakeBpeSample(normPath, sampPath, maxChars: 40_000_000);

            // Normalize lines before training tokenizer
            var lines = File.ReadAllLines(sampPath)
                            .Select(s => TextNormalizer.NormalizeForBpe(s))
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToArray();

            var bpe = BpeTokenizer.Train(
                lines,
                vocabSize: cfg.VocabSize,
                minCount: cfg.MinCount,
                log: s => Console.WriteLine(s),
                onMergeCheckpoint: (model, mergesSoFar) =>
                {
                    // only write if we advanced
                    if (mergesSoFar != lastSavedMerge)
                    {
                        var json = model.ToJson(true);
                        var tmp = tokPath + ".tmp_" + Guid.NewGuid().ToString("N");
                        File.WriteAllText(tmp, json);
                        if (File.Exists(tokPath)) File.Replace(tmp, tokPath, null); else File.Move(tmp, tokPath);
                        Console.WriteLine($"[ckpt] tokenizer saved @ merges={mergesSoFar:N0} → {tokPath}");
                        lastSavedMerge = mergesSoFar;
                    }
                },
                checkpointEvery: 4000 // tweak as you like (e.g., 100–500)
            );


            File.WriteAllText(tokPath, bpe.ToJson(true));
            Console.WriteLine($"Saved tokenizer → {tokPath}");

            var tok = new BpeTokenizer(bpe);

            // Encode corpus & train n-gram
            var seqs = NGramIO.EncodeLines(tok, lines);
            var lm = new NGramLm(order: cfg.NGramOrder, bosId: tok.BosId, eosId: tok.EosId, discount: 0.75f);

            //lm.Fit(seqs);

            long lastSaved = 0;
            var lmPath = Path.Combine(cfg.ModelsDir, "ngram-kn.json");
            lm.Fit(seqs, onProgress: seqCount =>
            {
                if (seqCount - lastSaved >= 100_000)
                {
                    lastSaved = seqCount;
                    SafeFileIO.AtomicWrite(lmPath, lm.ToJson(true));
                    Console.WriteLine($"[ckpt] n-gram saved @ {seqCount:N0} sequences → {lmPath}");
                }
            });
            // final save (already there, but we keep it):
            SafeFileIO.AtomicWrite(lmPath, lm.ToJson(true));
            Console.WriteLine($"Saved LM → {lmPath}");

            var ppl = lm.Perplexity(seqs);
            Console.WriteLine($"N-gram PPL (train-set quick check): {ppl:F2}");

            NGramIO.Save(lm, lmPath);
            Console.WriteLine($"Saved LM → {lmPath}");

            // Sample a bit
            var sample = LmSampler.Generate(tok, lm, prompt: "Who are you?", maxNewTokens: 30, seed: 42);
            Console.WriteLine("LM sample: " + sample);
        }

        private static void ExportLmLines(string conn, string outPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)) ?? ".");
            var sql = @"SELECT definition FROM dbo.synsets WHERE definition IS NOT NULL UNION ALL SELECT sample FROM dbo.samples WHERE sample IS NOT NULL;";

            using var sw = new StreamWriter(outPath);
            using var cn = new SqlConnection(conn);
            using var cmd = new SqlCommand(sql, cn);
            cn.Open();
            using var rd = cmd.ExecuteReader();
            long n = 0;
            using var prog = new ConsoleProgress("LM export", every: 10_000);
            while (rd.Read())
            {
                var line = rd.GetString(0);
                line = TextNormalizer.NormalizeForBpe(line);
                if (!string.IsNullOrWhiteSpace(line)) { sw.WriteLine(line); n++; prog.Step(n); }
            }
            prog.Done(n);
        }

        // -------------------------
        // REPORT: Tokenizer
        // -------------------------
        private static void TokReport(Config cfg)
        {
            var tokPath = Path.Combine(cfg.ModelsDir, "tokenizer.json");
            var textPath = Path.Combine(cfg.DataDir, "lm", "text.txt");
            if (!File.Exists(tokPath) || !File.Exists(textPath))
            {
                Console.WriteLine("Tokenizer report skipped (missing tokenizer.json or data/lm/text.txt). Run `lm` or `all` first.");
                return;
            }

            var outDir = Path.Combine(cfg.ReportsDir, "tok1");
            var lines = File.ReadAllLines(textPath).Select(s => TextNormalizer.NormalizeForBpe(s));
            TokenizerInspector.Analyze(tokPath, lines, outDir, topN: 200, sampleWords: 200);
            Console.WriteLine($"Tokenizer report → {outDir}");
        }

        // -------------------------
        // PIPELINE: WSD
        // -------------------------
        private static void WsdBuildAndTrain(Config cfg)
        {
            Console.WriteLine("== WSD: dataset + train ==");

            var ckptDir = Path.Combine(cfg.ModelsDir, "wsd");
            Directory.CreateDirectory(ckptDir);
            var modelPath = Path.Combine(ckptDir, "wsd.model.json");
            var statePath = Path.Combine(ckptDir, "wsd.state.json");

            var all = WsdDatasetBuilder.BuildWsdDataset(cfg.Conn, maxPerLemmaPos: cfg.MaxPerLemmaPos);
            if (all.Count == 0)
            {
                Console.WriteLine("No WSD examples found. Check your OEWN data.");
                return;
            }

            // Shuffle and split 90/10
            var rng = new Random(42);
            for (int i = all.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }
            int cut = (int)(all.Count * 0.9);
            var train = all.GetRange(0, cut);
            var dev = all.GetRange(cut, all.Count - cut);
            Console.WriteLine($"WSD dataset: train={train.Count}  dev={dev.Count}");

            using var inv = new SqlServerSenseInventory(cfg.Conn);
            PrewarmInventory(inv, train);

            // === Resume logic ===
            BiEncoderWsd model;
            var state = new WsdTrainState { EpochCompleted = 0 };
            if (File.Exists(modelPath) && File.Exists(statePath))
            {
                model = BiEncoderWsd.FromJson(File.ReadAllText(modelPath));
                state = System.Text.Json.JsonSerializer.Deserialize<WsdTrainState>(File.ReadAllText(statePath)) ?? state;
                Console.WriteLine($"[resume] Loaded WSD model, completed epochs = {state.EpochCompleted}");
            }
            else
            {
                model = new BiEncoderWsd(ctxFeatures: cfg.CtxFeat, glsFeatures: cfg.GlsFeat, projDim: cfg.ProjDim, seed: 42);
            }

            // Save-on-Ctrl+C
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // don’t kill immediately
                Console.WriteLine("\n[signal] Ctrl+C detected → saving WSD checkpoint…");
                SafeFileIO.AtomicWrite(modelPath, model.ToJson(true));
                SafeFileIO.AtomicWrite(statePath, System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine("[signal] Checkpoint saved. Exiting.");
                Environment.Exit(130);
            };

            for (int ep = state.EpochCompleted + 1; ep <= cfg.Epochs; ep++)
            {
                Console.WriteLine($"-- WSD epoch {ep}/{cfg.Epochs} --");
                var (loss, acc) = TrainEpochLoggedWithMidCkpt(model, train, inv, cfg.Lr, cfg.MaxCandidates,
                                                              modelPath, statePath, state, saveEvery: 10_000);
                var devAcc = WsdTrainer.Evaluate(model, dev, inv, cfg.MaxCandidates);
                Console.WriteLine($"WSD epoch {ep}/{cfg.Epochs}  loss={loss:F4}  trainAcc={acc:P1}  devAcc={devAcc:P1}");

                // end-of-epoch checkpoint
                state.EpochCompleted = ep;
                SafeFileIO.AtomicWrite(modelPath, model.ToJson(true));
                SafeFileIO.AtomicWrite(statePath, System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"[ckpt] Saved WSD epoch {ep} → {ckptDir}");
            }

            Console.WriteLine("WSD: training complete.");
        }

        // Progress-logged trainer (wraps WsdTrainer style)
        private static (double loss, double acc) TrainEpochLogged(
            BiEncoderWsd model, List<WsdExample> train, ISenseInventory inv, float lr, int maxCandidates)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            double lossSum = 0; long correct = 0, total = 0;
            int n = train.Count; int lastLog = 0;

            foreach (var ex in train)
            {
                var cands = inv.GetCandidates(ex.Lemma, ex.Pos);
                if (cands.Count == 0) continue;

                int m = Math.Min(cands.Count, maxCandidates);
                var glosses = new string[m];
                int gold = -1;
                for (int i = 0; i < m; i++)
                {
                    glosses[i] = cands[i].Gloss;
                    if (cands[i].SynsetId == ex.GoldSynsetId) gold = i;
                }
                if (gold < 0) continue;

                lossSum += model.TrainStep(ex.Tokens, ex.TargetIndex, glosses, gold, lr);
                int pred = model.Predict(ex.Tokens, ex.TargetIndex, glosses);
                if (pred == gold) correct++;
                total++;

                if (total - lastLog >= 1000)
                {
                    lastLog = (int)total;
                    var t = sw.Elapsed.TotalSeconds;
                    var rate = total / Math.Max(1.0, t);
                    var remaining = n - total;
                    var eta = TimeSpan.FromSeconds(remaining / Math.Max(1.0, rate));
                    Console.WriteLine($"  WSD progress: {total}/{n}  {rate:F1} ex/s  ETA ~ {eta:hh\\:mm\\:ss}");
                }
            }
            return (total == 0 ? 0 : lossSum / total, total == 0 ? 0 : (double)correct / total);
        }

        // Prewarm inventory cache to avoid SQL during hot loop
        private static void PrewarmInventory(ISenseInventory inv, IEnumerable<WsdExample> data)
        {
            var seen = new HashSet<(string, string)>();
            foreach (var ex in data)
            {
                var key = (ex.Lemma, ex.Pos);
                if (seen.Add(key)) _ = inv.GetCandidates(ex.Lemma, ex.Pos);
            }
            Console.WriteLine($"Prewarmed {seen.Count} lemma+POS candidate sets.");
        }

        private static (double loss, double acc) TrainEpochLoggedWithMidCkpt(BiEncoderWsd model, List<WsdExample> train, ISenseInventory inv, float lr, int maxCandidates, string modelPath, string statePath, WsdTrainState state, int saveEvery)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            double lossSum = 0; long correct = 0, total = 0; int n = train.Count; int lastLog = 0;

            foreach (var ex in train)
            {
                var cands = inv.GetCandidates(ex.Lemma, ex.Pos);
                if (cands.Count == 0) continue;

                int m = Math.Min(cands.Count, maxCandidates);
                var glosses = new string[m];
                int gold = -1;
                for (int i = 0; i < m; i++)
                {
                    glosses[i] = cands[i].Gloss;
                    if (cands[i].SynsetId == ex.GoldSynsetId) gold = i;
                }
                if (gold < 0) continue;

                lossSum += model.TrainStep(ex.Tokens, ex.TargetIndex, glosses, gold, lr);
                int pred = model.Predict(ex.Tokens, ex.TargetIndex, glosses);
                if (pred == gold) correct++;
                total++;

                if (total - lastLog >= 1000)
                {
                    lastLog = (int)total;
                    var t = sw.Elapsed.TotalSeconds;
                    var rate = total / Math.Max(1.0, t);
                    var eta = TimeSpan.FromSeconds((n - total) / Math.Max(1.0, rate));
                    Console.WriteLine($"  WSD progress: {total}/{n}  {rate:F1} ex/s  ETA ~ {eta:hh\\:mm\\:ss}");
                }

                if (total % saveEvery == 0)
                {
                    // mid-epoch checkpoint (same epochCompleted value)
                    SafeFileIO.AtomicWrite(modelPath, model.ToJson(true));
                    SafeFileIO.AtomicWrite(statePath, System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                    Console.WriteLine($"  [ckpt] mid-epoch save @ {total:N0}/{n:N0}");
                }
            }

            return (total == 0 ? 0 : lossSum / total, total == 0 ? 0 : (double)correct / total);
        }

        static string EnsureNormalizedCorpus(string rawPath, string normPath)
        {
            if (File.Exists(normPath)) return normPath;

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(normPath)) ?? ".");
            using var r = new StreamReader(rawPath);
            using var w = new StreamWriter(normPath);
            char[] buf = new char[1 << 16];
            var sb = new System.Text.StringBuilder(1 << 16);
            long lines = 0;
            while (true)
            {
                var s = r.ReadLine();
                if (s == null) break;
                var norm = AM.Mind.Text.TextNormalizer.NormalizeForBpe(s);
                if (norm.Length == 0) continue;
                w.WriteLine(norm);
                if ((++lines % 50_000) == 0) Console.WriteLine($"normalize: {lines:N0}");
            }
            Console.WriteLine($"normalize: DONE {lines:N0} lines → {normPath}");
            return normPath;
        }

        // Reservoir sample up to N chars for BPE
        static string MakeBpeSample(string normPath, string samplePath, long maxChars = 10_000_000)
        {
            if (File.Exists(samplePath)) return samplePath;

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(samplePath)) ?? ".");
            using var r = new StreamReader(normPath);
            using var w = new StreamWriter(samplePath);
            long total = 0;
            while (true)
            {
                var s = r.ReadLine();
                if (s == null) break;
                if (s.Length == 0) continue;
                if (total + s.Length + 1 > maxChars) break;
                w.WriteLine(s);
                total += s.Length + 1;
            }
            Console.WriteLine($"bpe-sample: {total:N0} chars → {samplePath}");
            return samplePath;
        }
    }
    internal sealed class ConsoleProgress : IDisposable
    {
        private readonly string _label;
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private long _n0 = 0;
        private long _lastN = 0;
        private double _lastT = 0;
        private readonly int _every;

        public ConsoleProgress(string label, int every = 2000, long start = 0)
        {
            _label = label; _every = Math.Max(1, every); _n0 = start;
        }

        public void Step(long n)
        {
            if (n % _every != 0 && n != _n0) return;
            var t = _sw.Elapsed.TotalSeconds;
            var dn = n - _lastN;
            var dt = Math.Max(1e-6, t - _lastT);
            var rate = dn / dt;
            Console.WriteLine($"{_label}: {n:N0}  {rate:N1}/s  t={t:0.0}s");
            _lastN = n; _lastT = t;
        }

        public void Done(long n, string? extra = null)
        {
            var t = _sw.Elapsed.TotalSeconds;
            Console.WriteLine($"{_label}: DONE {n:N0} in {t:0.0}s{(extra is null ? "" : "  " + extra)}");
        }

        public void Dispose() { _sw.Stop(); }
    }
    internal static class SafeFileIO
    {
        // Write to temp then atomically move over the destination.
        public static void AtomicWrite(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
            var tmp = path + ".tmp_" + Guid.NewGuid().ToString("N");
            File.WriteAllText(tmp, contents);
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }
    }
}