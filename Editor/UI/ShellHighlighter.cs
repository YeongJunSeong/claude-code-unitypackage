using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeCode.Editor.UI
{
    public static class ShellHighlighter
    {
        static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "if", "then", "else", "elif", "fi", "case", "esac", "for", "while", "do", "done",
            "function", "return", "in", "select", "until", "break", "continue", "exit",
            "export", "local", "readonly", "declare", "typeset", "set", "unset", "shift",
            "source", "true", "false"
        };

        static readonly HashSet<string> Builtins = new HashSet<string>
        {
            "echo", "printf", "read", "cd", "ls", "pwd", "mkdir", "rmdir", "rm", "cp", "mv",
            "cat", "head", "tail", "grep", "sed", "awk", "sort", "uniq", "wc", "tr", "cut",
            "find", "xargs", "which", "type", "alias", "unalias", "history", "time",
            "git", "npm", "node", "python", "python3", "pip", "pip3", "claude",
            "curl", "wget", "ssh", "scp", "rsync", "tar", "zip", "unzip",
            "sudo", "chmod", "chown", "ps", "kill", "killall", "top", "df", "du", "free",
            "env", "test", "true", "false", "exit"
        };

        static readonly Regex TokenRegex = new Regex(
            @"(?<comment>\#[^\n]*)" +
            @"|(?<string>""(?:\\.|[^""\\])*""|'[^']*')" +
            @"|(?<variable>\$\{[^}]+\}|\$\w+|\$[?#$@*!-])" +
            @"|(?<flag>(?<=\s|^)--?\w[\w-]*)" +
            @"|(?<number>\b\d+\b)" +
            @"|(?<operator>&&|\|\||[|&;><])" +
            @"|(?<word>\b[A-Za-z_][\w-]*\b)",
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
            if (m.Groups["string"].Success) return SyntaxHighlightUtil.ColorString;
            if (m.Groups["variable"].Success) return SyntaxHighlightUtil.ColorVariable;
            if (m.Groups["flag"].Success) return SyntaxHighlightUtil.ColorAttribute;
            if (m.Groups["number"].Success) return SyntaxHighlightUtil.ColorNumber;
            if (m.Groups["operator"].Success) return SyntaxHighlightUtil.ColorControl;
            if (m.Groups["word"].Success)
            {
                var word = m.Value;
                if (Keywords.Contains(word)) return SyntaxHighlightUtil.ColorKeyword;
                if (Builtins.Contains(word)) return SyntaxHighlightUtil.ColorMethod;
            }
            return null;
        }

        public static bool IsShellLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang)) return false;
            switch (lang.ToLowerInvariant())
            {
                case "sh":
                case "bash":
                case "shell":
                case "zsh":
                case "powershell":
                case "ps1":
                case "cmd":
                case "bat":
                    return true;
                default:
                    return false;
            }
        }
    }
}
