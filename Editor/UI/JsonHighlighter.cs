using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeCode.Editor.UI
{
    public static class JsonHighlighter
    {
        static readonly Regex TokenRegex = new Regex(
            @"(?<comment>//[^\n]*|/\*[\s\S]*?\*/)" +
            @"|(?<string>""(?:\\.|[^""\\])*"")" +
            @"|(?<number>-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b)" +
            @"|(?<keyword>\b(?:true|false|null)\b)",
            RegexOptions.Compiled);

        public static string Highlight(string code)
        {
            if (string.IsNullOrEmpty(code)) return code;

            var sb = new StringBuilder(code.Length + 256);
            int pos = 0;

            foreach (Match m in TokenRegex.Matches(code))
            {
                if (m.Index > pos)
                    SyntaxHighlightUtil.AppendEscaped(sb, code, pos, m.Index - pos);

                var color = DetermineColor(m, code);
                SyntaxHighlightUtil.AppendToken(sb, m.Value, color);
                pos = m.Index + m.Length;
            }

            if (pos < code.Length)
                SyntaxHighlightUtil.AppendEscaped(sb, code, pos, code.Length - pos);

            return sb.ToString();
        }

        static string DetermineColor(Match m, string code)
        {
            if (m.Groups["comment"].Success) return SyntaxHighlightUtil.ColorComment;
            if (m.Groups["string"].Success)
            {
                // Key detection: string followed by ':' (after whitespace) → property name color
                int endIdx = m.Index + m.Length;
                while (endIdx < code.Length && char.IsWhiteSpace(code[endIdx])) endIdx++;
                if (endIdx < code.Length && code[endIdx] == ':')
                    return SyntaxHighlightUtil.ColorProperty;
                return SyntaxHighlightUtil.ColorString;
            }
            if (m.Groups["number"].Success) return SyntaxHighlightUtil.ColorNumber;
            if (m.Groups["keyword"].Success) return SyntaxHighlightUtil.ColorKeyword;
            return null;
        }

        public static bool IsJsonLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang)) return false;
            switch (lang.ToLowerInvariant())
            {
                case "json":
                case "jsonc":
                case "json5":
                    return true;
                default:
                    return false;
            }
        }
    }
}
