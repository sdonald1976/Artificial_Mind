using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AM.Mind.WSD;

// Build a WSD training/dev set from your DB
// using AM.Mind.WSD;
// using System.Text.RegularExpressions;
public static class WsdDatasetBuilder
{

    public static List<WsdExample> BuildWsdDataset(string conn, int maxPerLemmaPos = 5000)
    {
        var list = new List<WsdExample>();
        var sql = @"SELECT TOP (200000) sa.sample, sa.wordid, sy.synsetid, sy.posid, w.word FROM dbo.samples AS sa JOIN dbo.synsets AS sy ON sy.synsetid = sa.synsetid LEFT JOIN dbo.words   AS w  ON w.wordid  = sa.wordid WHERE sa.sample IS NOT NULL ORDER BY sa.sampleid;";

        using var cn = new SqlConnection(conn);
        using var cmd = new SqlCommand(sql, cn);
        cn.Open();
        using var rd = cmd.ExecuteReader();
        var cap = new Dictionary<(string lemma,string pos), int>();

        while (rd.Read())
        {
            string sent = rd.IsDBNull(0) ? "" : rd.GetString(0);
            bool hasWid = !rd.IsDBNull(1);
            int synset = rd.GetInt32(2);
            string pos = rd.GetString(3); // 'n','v','a','r'
            string lemma = hasWid ? rd.GetString(4) : null;

            // fallback: if no lemma, skip for now (or infer later)
            if (string.IsNullOrWhiteSpace(sent) || string.IsNullOrWhiteSpace(lemma)) continue;

            var key = (lemma, pos);
            if (!cap.TryGetValue(key, out var used)) used = 0;
            if (used >= maxPerLemmaPos) continue;

            // very simple tokenization
            var tokens = Regex.Split(sent.Trim(), @"\s+");
            int targetIdx = Array.FindIndex(tokens, t => string.Equals(t.TrimEnd(new char[] { '\'', '.', ',', ';', ':', '!', '?', ')', '(' }).ToLowerInvariant(), lemma, StringComparison.Ordinal));
            if (targetIdx < 0) continue;

            list.Add(new WsdExample(tokens, targetIdx, lemma, pos, goldSynsetId: synset.ToString()));
            cap[key] = used + 1;
        }

        return list;
    }
}