using AM.Mind.Interfaces;
using AM.Mind.IO;
using AM.Mind.IO.Models;
using AM.Mind.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Models;

public sealed class XLogExperienceSink<TObs, TAct> : IExperienceSink<TObs, TAct>
{
    private readonly Func<Experience<TObs, TAct>, ExperienceEnvelope> _toEnvelope;
    private readonly XLogWriter _writer;

    public XLogExperienceSink(XLogWriter writer,
                              Func<Experience<TObs, TAct>, ExperienceEnvelope> toEnvelope)
    {
        _writer = writer;
        _toEnvelope = toEnvelope;
    }

    public void Append(in Experience<TObs, TAct> exp)
    {
        var env = _toEnvelope(exp);
        _writer.Append(env);
        // optional: batching/flush policy belongs here, not in the brain.
    }
}
