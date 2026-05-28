using System.IO;
using UnityEngine;

namespace ClaudeCode.Editor.MCP
{
    public static class McpConfigWriter
    {
        public const string ServerName = "unity";
        public const string PermissionToolFullName = "mcp__unity__permission_prompt";

        public static string GetConfigPath()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, ".claudecode-mcp.json");
        }

        public static string WriteConfig(int port)
        {
            var configPath = GetConfigPath();
            var url = $"http://localhost:{port}/";
            var json = "{\n" +
                       "  \"mcpServers\": {\n" +
                       $"    \"{ServerName}\": {{\n" +
                       "      \"type\": \"http\",\n" +
                       $"      \"url\": \"{url}\"\n" +
                       "    }\n" +
                       "  }\n" +
                       "}\n";

            File.WriteAllText(configPath, json);
            return configPath;
        }

        public static void DeleteConfig()
        {
            var configPath = GetConfigPath();
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }
}
