using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

public interface ITextEncoder
{
    int FeatureCount { get; }
    void Encode(string text, float[] dst); // dst.Length == FeatureCount
}
