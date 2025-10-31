using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Interfaces;

// Snapshot (policy / config persistence)
public interface ISnapshotStore
{
    void Save<T>(string path, T obj);
    T Load<T>(string path);
}
