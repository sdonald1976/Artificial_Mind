using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Models;

public sealed class PosTagSet
{
    private readonly Dictionary<string,int> _toId = new(StringComparer.Ordinal);
    private readonly List<string> _toTag = new();

    public int Count => _toTag.Count;

    public int GetId(string tag)
    {
        if (_toId.TryGetValue(tag, out var id)) return id;
        id = _toTag.Count;
        _toId[tag] = id;
        _toTag.Add(tag);
        return id;
    }

    public string GetTag(int id) => (id >= 0 && id < _toTag.Count) ? _toTag[id] : "UNK";
}
