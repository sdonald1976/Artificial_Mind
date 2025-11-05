using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AM.Mind.Text;

public sealed class Vocab
{
    public const string DefaultPad = "<pad>";
    public const string DefaultUnk = "<unk>";
    public const string DefaultBos = "<bos>";
    public const string DefaultEos = "<eos>";

    [JsonInclude] public List<string> IdToToken { get; private set; } = new();
    [JsonIgnore] public Dictionary<string, int> TokenToId { get; private set; } = new(StringComparer.Ordinal);

    [JsonInclude] public int PadId { get; private set; }
    [JsonInclude] public int UnkId { get; private set; }
    [JsonInclude] public int BosId { get; private set; }
    [JsonInclude] public int EosId { get; private set; }

    public int Size => IdToToken.Count;

    public static Vocab FromTokens(List<string> tokens, string pad = DefaultPad, string unk = DefaultUnk, string bos = DefaultBos, string eos = DefaultEos)
    {
        var v = new Vocab();
        v.IdToToken.AddRange(new[] { pad, unk, bos, eos });
        foreach (var t in tokens) v.IdToToken.Add(t);
        v.RebuildMap();

        v.PadId = v.TokenToId[pad];
        v.UnkId = v.TokenToId[unk];
        v.BosId = v.TokenToId[bos];
        v.EosId = v.TokenToId[eos];
        return v;
    }

    public void RebuildMap()
    {
        TokenToId.Clear();
        for (int i = 0; i < IdToToken.Count; i++)
            if (!TokenToId.ContainsKey(IdToToken[i]))
                TokenToId[IdToToken[i]] = i;
    }

    public int this[string token] => TokenToId.TryGetValue(token, out var id) ? id : UnkId;
    public string this[int id] => (id >= 0 && id < IdToToken.Count) ? IdToToken[id] : DefaultUnk;

    public string ToJson(bool indented = true)
        => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = indented });

    public static Vocab FromJson(string json)
    {
        var v = JsonSerializer.Deserialize<Vocab>(json)!;
        v.RebuildMap();
        return v;
    }
}


