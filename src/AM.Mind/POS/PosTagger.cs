using AM.Mind.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.POS;

public sealed class PosTagger
{
    private readonly PosFeaturizer _feat;
    private readonly SoftmaxClassifier _clf;
    private readonly PosTagSet _tags;
    private readonly float[] _x; // feature buffer

    public PosTagger(PosFeaturizer feat, PosTagSet tags, int seed = 123)
    {
        _feat = feat;
        _tags = tags;
        _clf = new SoftmaxClassifier(_feat.FeatureDim, _tags.Count, seed);
        _x = new float[_feat.FeatureDim];
    }

    public int PredictToken(string[] tokens, int i)
    {
        _feat.Encode(tokens, i, _x);
        return _clf.Predict(_x);
    }

    public string[] PredictSentence(string[] tokens)
    {
        var outTags = new string[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            int id = PredictToken(tokens, i);
            outTags[i] = _tags.GetTag(id);
        }
        return outTags;
    }

    public (double loss, double acc) TrainEpoch(List<PosSentence> train, float lr = 0.05f)
    {
        double loss = 0; long correct = 0, total = 0;
        foreach (var s in train)
        {
            for (int i = 0; i < s.Tokens.Length; i++)
            {
                _feat.Encode(s.Tokens, i, _x);
                int y = _tags.GetId(s.Tags[i]); // will expand if unseen
                loss += _clf.TrainStep(_x, y, lr);
                int p = _clf.Predict(_x);
                if (p == y) correct++;
                total++;
            }
        }
        return (loss / Math.Max(1, total), total == 0 ? 0 : (double)correct / total);
    }

    public double Evaluate(List<PosSentence> test)
    {
        long correct = 0, total = 0;
        foreach (var s in test)
        {
            for (int i = 0; i < s.Tokens.Length; i++)
            {
                _feat.Encode(s.Tokens, i, _x);
                int y = _tags.GetId(s.Tags[i]);
                int p = _clf.Predict(_x);
                if (p == y) correct++;
                total++;
            }
        }
        return total == 0 ? 0 : (double)correct / total;
    }
}