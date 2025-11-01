using AM.Mind.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Features;

public sealed class IdentityScaler : IFeatureScaler
{
    public void Transform(ReadOnlySpan<float> src, Span<float> dst) => src.CopyTo(dst);
}
