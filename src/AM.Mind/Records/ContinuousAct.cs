using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Records;

public readonly record struct ContinuousAct(ReadOnlyMemory<float> Values);
