using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Text;

public static class TextNormalizer
{
    // Remove parentheses and similar bracket chars; lowercase; NFKC.
    // You can extend the forbidden set if needed.
    private static readonly string Forbidden = "()[]{}<>";

    public static string NormalizeForBpe(string s, bool stripDiacritics = false)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";

        // Unicode normalize (compat)
        s = s.Normalize(NormalizationForm.FormKC).ToLowerInvariant();

        // Strip diacritics if desired
        if (stripDiacritics)
        {
            var f = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(f.Length);
            foreach (var ch in f)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            s = sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // Remove forbidden bracket chars and collapse whitespace
        {
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (Forbidden.IndexOf(ch) >= 0) continue; // drop (),[],{},<>
                sb.Append(char.IsWhiteSpace(ch) ? ' ' : ch);
            }
            s = sb.ToString();
        }

        // collapse multiple spaces
        s = CollapseSpaces(s);
        return s.Trim();
    }

    public static string NormalizeWord(string w)
    {
        if (string.IsNullOrEmpty(w)) return "";
        var sb = new StringBuilder(w.Length);
        foreach (var ch in w)
        {
            if ("()[]{}<>".IndexOf(ch) >= 0) continue; // drop parentheses inside words
            sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string CollapseSpaces(string s)
    {
        var sb = new StringBuilder(s.Length);
        bool prevSpace = false;
        foreach (var ch in s)
        {
            bool isSpace = ch == ' ';
            if (isSpace)
            {
                if (!prevSpace) sb.Append(' ');
            }
            else sb.Append(ch);
            prevSpace = isSpace;
        }
        return sb.ToString();
    }
}