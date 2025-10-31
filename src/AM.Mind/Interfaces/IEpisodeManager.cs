using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

// Episode/time structure
public interface IEpisodeManager
{
    int CurrentEpisode { get; }
    int Step { get; }
    bool Terminal { get; }

    void Begin();
    void Next();
    void End();
}
