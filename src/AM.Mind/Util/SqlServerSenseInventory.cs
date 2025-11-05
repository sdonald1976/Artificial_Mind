using AM.Mind.Interfaces;
using AM.Mind.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Util;

// SqlServerSenseInventory tuned to your schema
// using AM.Mind.WSD;
// using System.Data;
// using System.Data.SqlClient;

public sealed class SqlServerSenseInventory : ISenseInventory, IDisposable
{
    private readonly string _conn;
    private readonly Dictionary<(string, string), List<SenseCandidate>> _cache = new();

    private const string _sql = @"SELECT TOP (64) sy.synsetid, CAST(sy.definition AS nvarchar(max)) AS Gloss, NULL AS Examples FROM dbo.senses AS se JOIN dbo.synsets AS sy ON sy.synsetid = se.synsetid JOIN dbo.words   AS w  ON w.wordid    = se.wordid WHERE w.word = @lemma AND sy.posid = @pos ORDER BY ISNULL(se.sensenum, 0);";

    public SqlServerSenseInventory(string conn) { _conn = conn; }

    public List<SenseCandidate> GetCandidates(string lemma, string pos)
    {
        var key = (lemma, pos);
        if (_cache.TryGetValue(key, out var hit)) return new(hit);

        var list = new List<SenseCandidate>();
        using var cn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(_sql, cn);
        cmd.Parameters.Add(new SqlParameter("@lemma", SqlDbType.NVarChar, 128) { Value = lemma });
        cmd.Parameters.Add(new SqlParameter("@pos", SqlDbType.NVarChar, 8) { Value = pos });
        cn.Open();
        using var rd = cmd.ExecuteReader();
        while (rd.Read())
        {
            string syn = rd.GetInt32(0).ToString();      // synsetId
            string gloss = rd.IsDBNull(1) ? "" : rd.GetString(1);
            var ex = Array.Empty<string>();
            list.Add(new SenseCandidate(syn, gloss, ex));
        }
        _cache[key] = list;
        return new(list);
    }

    public void Dispose() { }
}