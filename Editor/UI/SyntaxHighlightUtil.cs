using System.Text;

namespace ClaudeCode.Editor.UI
{
    /// <summary>
    /// Shared utilities for syntax highlighters.
    /// All highlighters output Unity rich-text strings that go into a Label with enableRichText = true.
    /// </summary>
    public static class SyntaxHighlightUtil
    {
        // VS Dark+ inspired shared color palette.
        public const string ColorKeyword   = "#569CD6";
        public const string ColorControl   = "#C586C0";
        public const string ColorType      = "#4EC9B0";
        public const string ColorMethod    = "#DCDCAA";
        public const string ColorString    = "#CE9178";
        public const string ColorNumber    = "#B5CEA8";
        public const string ColorComment   = "#6A9955";
        public const string ColorAttribute = "#DCDCAA";
        public const string ColorPreproc   = "#BD63C5";
        public const string ColorProperty  = "#9CDCFE"; // JSON keys, YAML keys
        public const string ColorVariable  = "#9CDCFE"; // shell variables
        public const string ColorOperator  = "#D4D4D4"; // default text

        /// <summary>
        /// Append text to the StringBuilder, escaping characters that would break Unity rich text.
        /// </summary>
        public static void AppendEscaped(StringBuilder sb, string s, int start, int len)
        {
            int end = start + len;
            for (int i = start; i < end; i++)
            {
                char c = s[i];
                if (c == '<') sb.Append("<noparse><</noparse>");
                else sb.Append(c);
            }
        }

        public static void AppendEscaped(StringBuilder sb, string s)
        {
            AppendEscaped(sb, s ?? string.Empty, 0, s?.Length ?? 0);
        }

        /// <summary>
        /// Append a token wrapped in a color tag (or plain-escaped if color is null).
        /// </summary>
        public static void AppendToken(StringBuilder sb, string token, string color)
        {
            if (color == null)
            {
                AppendEscaped(sb, token);
                return;
            }
            sb.Append("<color=").Append(color).Append('>');
            AppendEscaped(sb, token);
            sb.Append("</color>");
        }
    }
}
