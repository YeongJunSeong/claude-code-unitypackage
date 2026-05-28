using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ClaudeCode.Editor.MCP
{
    public static class McpJsonParser
    {
        public static object Parse(string json)
        {
            int pos = 0;
            return ParseValue(json, ref pos);
        }

        public static JsonRpcRequest ParseRequest(string json)
        {
            var obj = Parse(json) as Dictionary<string, object>;
            if (obj == null) return null;

            var req = new JsonRpcRequest
            {
                method = obj.TryGetValue("method", out var m) ? m?.ToString() : null,
                id = obj.TryGetValue("id", out var i) ? i?.ToString() : null
            };

            if (obj.TryGetValue("params", out var p) && p is Dictionary<string, object> dict)
                req.@params = dict;

            return req;
        }

        static object ParseValue(string s, ref int pos)
        {
            SkipWhitespace(s, ref pos);
            if (pos >= s.Length) return null;

            char c = s[pos];
            if (c == '{') return ParseObject(s, ref pos);
            if (c == '[') return ParseArray(s, ref pos);
            if (c == '"') return ParseString(s, ref pos);
            if (c == 't' || c == 'f') return ParseBool(s, ref pos);
            if (c == 'n') { pos += 4; return null; }
            return ParseNumber(s, ref pos);
        }

        static Dictionary<string, object> ParseObject(string s, ref int pos)
        {
            var dict = new Dictionary<string, object>();
            pos++;
            SkipWhitespace(s, ref pos);
            if (pos < s.Length && s[pos] == '}') { pos++; return dict; }

            while (pos < s.Length)
            {
                SkipWhitespace(s, ref pos);
                var key = ParseString(s, ref pos);
                SkipWhitespace(s, ref pos);
                pos++;
                var value = ParseValue(s, ref pos);
                dict[key] = value;
                SkipWhitespace(s, ref pos);
                if (pos < s.Length && s[pos] == ',') { pos++; continue; }
                if (pos < s.Length && s[pos] == '}') { pos++; break; }
            }

            return dict;
        }

        static List<object> ParseArray(string s, ref int pos)
        {
            var list = new List<object>();
            pos++;
            SkipWhitespace(s, ref pos);
            if (pos < s.Length && s[pos] == ']') { pos++; return list; }

            while (pos < s.Length)
            {
                list.Add(ParseValue(s, ref pos));
                SkipWhitespace(s, ref pos);
                if (pos < s.Length && s[pos] == ',') { pos++; continue; }
                if (pos < s.Length && s[pos] == ']') { pos++; break; }
            }

            return list;
        }

        static string ParseString(string s, ref int pos)
        {
            var sb = new StringBuilder();
            pos++;
            while (pos < s.Length)
            {
                char c = s[pos++];
                if (c == '"') return sb.ToString();
                if (c == '\\' && pos < s.Length)
                {
                    char esc = s[pos++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (pos + 4 <= s.Length)
                            {
                                var hex = s.Substring(pos, 4);
                                pos += 4;
                                sb.Append((char)int.Parse(hex, NumberStyles.HexNumber));
                            }
                            break;
                    }
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        static object ParseNumber(string s, ref int pos)
        {
            int start = pos;
            while (pos < s.Length && (char.IsDigit(s[pos]) || s[pos] == '-' || s[pos] == '+' || s[pos] == '.' || s[pos] == 'e' || s[pos] == 'E'))
                pos++;
            var num = s.Substring(start, pos - start);
            if (num.Contains(".") || num.Contains("e") || num.Contains("E"))
                return double.Parse(num, CultureInfo.InvariantCulture);
            return int.Parse(num, CultureInfo.InvariantCulture);
        }

        static bool ParseBool(string s, ref int pos)
        {
            if (s[pos] == 't') { pos += 4; return true; }
            pos += 5;
            return false;
        }

        static void SkipWhitespace(string s, ref int pos)
        {
            while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++;
        }
    }
}
