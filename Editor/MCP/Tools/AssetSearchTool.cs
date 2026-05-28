using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace ClaudeCode.Editor.MCP.Tools
{
    public class AssetSearchTool : IMcpTool
    {
        public string Name => "unity_asset_search";
        public string Description => "Search for assets in the Unity project by name, type, or label. Returns asset paths and types.";

        public McpInputSchema InputSchema => new McpInputSchema
        {
            properties = new Dictionary<string, McpPropertyDef>
            {
                ["query"] = new McpPropertyDef { type = "string", description = "Search query (e.g., 'Player t:Prefab', 't:Material', 'l:UI')" },
                ["max_results"] = new McpPropertyDef { type = "string", description = "Maximum results to return (default: 30)" }
            },
            required = new List<string> { "query" }
        };

        public string Execute(Dictionary<string, object> args)
        {
            var query = args.ContainsKey("query") ? args["query"]?.ToString() : "";
            var maxStr = args.ContainsKey("max_results") ? args["max_results"]?.ToString() : "30";
            int.TryParse(maxStr, out int maxResults);
            if (maxResults <= 0) maxResults = 30;

            var guids = AssetDatabase.FindAssets(query);
            var sb = new StringBuilder();
            sb.AppendLine($"Found {guids.Length} assets (showing max {maxResults}):");

            for (int i = 0; i < guids.Length && i < maxResults; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                sb.AppendLine($"  {path} [{type?.Name ?? "Unknown"}]");
            }

            if (guids.Length > maxResults)
                sb.AppendLine($"  ... and {guids.Length - maxResults} more");

            return sb.ToString();
        }
    }
}
