using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Config;

public sealed class AgentConfig
{
    public int StateDim { get; init; } = 4;
    public int ActionDim { get; init; } = 3;
    public int Seed { get; init; } = 42;
    public int FlushEvery { get; init; } = 64;
    public long RotateBytes { get; init; } = 256L * 1024 * 1024;
    public int BufferedQueueCapacity { get; init; } = 4096;
    public bool DropWhenFull { get; init; } = false;
    public string DataDir { get; init; } = "data";
    public string Prefix { get; init; } = "exp";
}
