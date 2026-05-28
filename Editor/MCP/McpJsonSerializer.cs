using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ClaudeCode.Editor.MCP
{
    public static class McpJsonSerializer
    {
        public static string SerializeResponse(string id, object result, JsonRpcError error)
        {
            var sb = new StringBuilder();
            sb.Append("{\"jsonrpc\":\"2.0\"");
            if (id != null) { sb.Append(",\"id\":"); AppendValue(sb, id); }
            if (result != null) { sb.Append(",\"result\":"); AppendValue(sb, result); }
            if (error != null)
            {
                sb.Append(",\"error\":{\"code\":");
                sb.Append(error.code);
                sb.Append(",\"message\":");
                AppendString(sb, error.message);
                sb.Append("}");
            }
            sb.Append("}");
            return sb.ToString();
        }

        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            AppendValue(sb, value);
            return sb.ToString();
        }

        static void AppendValue(StringBuilder sb, object value)
        {
            if (value == null) { sb.Append("null"); return; }

            switch (value)
            {
                case string s: AppendString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case float f: sb.Append(f.ToString(CultureInfo.InvariantCulture)); break;
                case double d: sb.Append(d.ToString(CultureInfo.InvariantCulture)); break;
                case IDictionary<string, object> dict: AppendDict(sb, dict); break;
                case McpToolDefinition toolDef: AppendToolDef(sb, toolDef); break;
                case IEnumerable list: AppendList(sb, list); break;
                default: AppendString(sb, value.ToString()); break;
            }
        }

        static void AppendDict(StringBuilder sb, IDictionary<string, object> dict)
        {
            sb.Append("{");
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(",");
                AppendString(sb, kv.Key);
                sb.Append(":");
                AppendValue(sb, kv.Value);
                first = false;
            }
            sb.Append("}");
        }

        static void AppendList(StringBuilder sb, IEnumerable list)
        {
            sb.Append("[");
            bool first = true;
            foreach (var item in list)
            {
                if (!first) sb.Append(",");
                AppendValue(sb, item);
                first = false;
            }
            sb.Append("]");
        }

        static void AppendToolDef(StringBuilder sb, McpToolDefinition tool)
        {
            sb.Append("{\"name\":");
            AppendString(sb, tool.name);
            sb.Append(",\"description\":");
            AppendString(sb, tool.description);
            sb.Append(",\"inputSchema\":");
            AppendInputSchema(sb, tool.inputSchema);
            sb.Append("}");
        }

        static void AppendInputSchema(StringBuilder sb, McpInputSchema schema)
        {
            sb.Append("{\"type\":\"object\",\"properties\":{");
            if (schema?.properties != null)
            {
                bool first = true;
                foreach (var kv in schema.properties)
                {
                    if (!first) sb.Append(",");
                    AppendString(sb, kv.Key);
                    sb.Append(":{\"type\":");
                    AppendString(sb, kv.Value.type);
                    sb.Append(",\"description\":");
                    AppendString(sb, kv.Value.description);
                    sb.Append("}");
                    first = false;
                }
            }
            sb.Append("}");
            if (schema?.required != null && schema.required.Count > 0)
            {
                sb.Append(",\"required\":[");
                for (int i = 0; i < schema.required.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    AppendString(sb, schema.required[i]);
                }
                sb.Append("]");
            }
            sb.Append("}");
        }

        static void AppendString(StringBuilder sb, string s)
        {
            if (s == null) { sb.Append("null"); return; }
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append($"\\u{(int)c:X4}");
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
