using AM.Mind.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Policies;

public static class PolicyPersistence
{
    public static void Save(LinearSoftmaxPolicy policy, string path, ISnapshotStore store)
    {
        var snap = policy.Export();
        store.Save(path, snap);
    }

    public static void Load(LinearSoftmaxPolicy policy, string path, ISnapshotStore store)
    {
        var snap = store.Load<LinearSoftmaxPolicy.Snapshot>(path);
        policy.Import(snap);
    }
}
