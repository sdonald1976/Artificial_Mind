using AM.Mind.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Text;

public static class TokenizerIO
{
    public static void Save(BpeTokenizer tokenizer, BpeModel model, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
        File.WriteAllText(path, model.ToJson());
    }

    public static BpeTokenizer Load(string path)
    {
        var json = File.ReadAllText(path);
        var model = BpeModel.FromJson(json);
        return new BpeTokenizer(model);
    }
}
