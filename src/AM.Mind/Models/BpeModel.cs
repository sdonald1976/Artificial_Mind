using AM.Mind.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AM.Mind.Models;

// On-disk tokenizer model (v1)
public sealed class BpeModel
{
    public string Version { get; set; } = "bpe-v1";
    public Vocab Vocab { get; set; } = new();
    public List<(string, string)> Merges { get; set; } = new(); // ordered pairs
    public string ToJson(bool indented = false)
    {
        var dto = new Serializable {
            Version = Version,
            Vocab = Vocab,
            Merges = Merges.Select(m => $"{m.Item1}\t{m.Item2}").ToList()
        };
        return System.Text.Json.JsonSerializer.Serialize(dto,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = indented });
    }

    public static BpeModel FromJson(string json)
    {
        var dto = System.Text.Json.JsonSerializer.Deserialize<Serializable>(json)!;
        return new BpeModel {
            Version = dto.Version,
            Vocab = dto.Vocab,
            Merges = dto.Merges.Select(s => {
                var i = s.IndexOf('\t'); 
                return (s[..i], s[(i+1)..]);
            }).ToList()
        };
    }

    private sealed class Serializable
    {
        public string Version { get; set; } = "bpe-v1";
        public Vocab Vocab { get; set; } = new Vocab();
        public List<string> Merges { get; set; } = new();
    }
}