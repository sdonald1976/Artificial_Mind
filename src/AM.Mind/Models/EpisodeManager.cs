using AM.Mind.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Models;

public sealed class EpisodeManager : IEpisodeManager
{
    public int CurrentEpisode { get; private set; }
    public int Step { get; private set; }
    public bool Terminal { get; private set; }

    public void Begin() { CurrentEpisode++; Step = 0; Terminal = false; }
    public void End()   { Terminal = true; }
    public void Next()  { Step++; }
}
