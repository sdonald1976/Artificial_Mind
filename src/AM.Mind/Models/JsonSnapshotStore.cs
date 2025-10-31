using AM.Mind.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AM.Mind.Models;

// Simple JSON snapshot store (text + UTF-8)
public sealed class JsonSnapshotStore : ISnapshotStore
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        WriteIndented = true
    };

    public void Save<T>(string path, T obj)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
        var json = JsonSerializer.Serialize(obj, Opt);
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    public T Load<T>(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<T>(json, Opt)!;
    }
}