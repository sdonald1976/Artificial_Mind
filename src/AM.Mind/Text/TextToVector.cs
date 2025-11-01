using AM.Mind.Interfaces;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Text;

public static class TextToVector
{
    public static VectorObs Encode(ITextEncoder enc, string text)
    {
        var buf = new float[enc.FeatureCount];
        enc.Encode(text, buf);
        return new VectorObs(buf);
    }
}
