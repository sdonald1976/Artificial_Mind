using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

public interface IFeatureScaler
{
    void Transform(ReadOnlySpan<float> src, Span<float> dst);
}

