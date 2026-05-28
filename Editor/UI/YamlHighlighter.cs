using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeCode.Editor.UI
{
    public static class YamlHighlighter
    {
        // YAML is whitespace-sensitive but for highlighting we just tokenize line by line.
        static readonly Regex TokenRegex = new Regex(
            @"(?<comment>\#[^\n]*)" +
            @"|(?<string>""(?:\\.|[^""\\\n])*""|'(?:''|[^'\n])*')" +
            @"|(?<keyword>\b(?:true|false|null|yes|no|on|off|True|False|Null|Yes|No|On|Off|TRUE|FALSE|NULL)\b)" +
            @"|(?<number>-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b)" +
            @"|(?<anchor>[&*][\w-]+)" +
            @"|(?<directive>^%\w+[^\n]*)" +
            @"|(?<key>(?<=^|\n)[ \t-]*[A-Za-z_][\w-]*(?=\s*:))" +
            @"|(?<docsep>^---|^\.\.\.)",
            RegexOptions.Compiled | RegexOptions.Multiline);

        public static string Highlight(string code)
        {
            if (string.IsNullOrEmpty(code)) return code;

            var sb = new StringBuilder(code.Length + 256);
            int pos = 0;

            foreach (Match m in TokenRegex.Matches(code))
            {
                if (m.Index > pos)
                    SyntaxHighlightUtil.AppendEscaped(sb, code, pos, m.Index - pos);

                var color = DetermineColor(m);
                SyntaxHighlightUtil.AppendToken(sb, m.Value, color);
                pos = m.Index + m.Length;
            }

            if (pos < code.Length)
                SyntaxHighlightUtil.AppendEscaped(sb, code, pos, code.Length - pos);

            return sb.ToString();
        }

        static string DetermineColor(Match m)
        {
            if (m.Groups["comment"].Success) return SyntaxHighlightUtil.ColorComment;
            if (m.Groups["string"].Success) return SyntaxHighlightUtil.ColorString;
            if (m.Groups["keyword"].Success) return SyntaxHighlightUtil.ColorKeyword;
            if (m.Groups["number"].Success) return SyntaxHighlightUtil.ColorNumber;
            if (m.Groups["anchor"].Success) return SyntaxHighlightUtil.ColorType;
            if (m.Groups["directive"].Success) return SyntaxHighlightUtil.ColorPreproc;
            if (m.Groups["key"].Success) return SyntaxHighlightUtil.ColorProperty;
            if (m.Groups["docsep"].Success) return SyntaxHighlightUtil.ColorControl;
            return null;
        }

        public static bool IsYamlLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang)) return false;
            switch (lang.ToLowerInvariant())
            {
                case "yaml":
                case "yml":
                    return true;
                default:
                    return false;
            }
        }
    }
}
