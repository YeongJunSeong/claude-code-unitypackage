using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClaudeCode.Editor.MCP.Tools
{
    public class SceneQueryTool : IMcpTool
    {
        public string Name => "unity_scene_query";
        public string Description => "Query the current Unity scene hierarchy. Returns GameObject names, components, and properties. Use 'query' to search by name pattern.";

        public McpInputSchema InputSchema => new McpInputSchema
        {
            properties = new Dictionary<string, McpPropertyDef>
            {
                ["query"] = new McpPropertyDef { type = "string", description = "Name pattern to search for (case-insensitive substring match). Leave empty to get root objects." },
                ["include_components"] = new McpPropertyDef { type = "string", description = "Set to 'true' to include component details." }
            },
            required = new List<string>()
        };

        public string Execute(Dictionary<string, object> args)
        {
            var query = args != null && args.ContainsKey("query") ? args["query"]?.ToString() : "";
            var includeComponents = args != null && args.ContainsKey("include_components") && args["include_components"]?.ToString() == "true";

            var scene = SceneManager.GetActiveScene();
            var sb = new StringBuilder();
            sb.AppendLine($"Scene: {scene.name}");

            if (string.IsNullOrEmpty(query))
            {
                foreach (var root in scene.GetRootGameObjects())
                    AppendGameObject(sb, root, 0, includeComponents, 2);
            }
            else
            {
                var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                int count = 0;
                foreach (var go in allObjects)
                {
                    if (go.name.ToLower().Contains(query.ToLower()))
                    {
                        AppendGameObject(sb, go, 0, includeComponents, 0);
                        count++;
                        if (count >= 50) { sb.AppendLine("... (truncated, too many results)"); break; }
                    }
                }
                if (count == 0) sb.AppendLine("No objects found matching query.");
            }

            return sb.ToString();
        }

        void AppendGameObject(StringBuilder sb, GameObject go, int depth, bool includeComponents, int maxChildDepth)
        {
            var indent = new string(' ', depth * 2);
            sb.AppendLine($"{indent}- {go.name} (active={go.activeSelf}, layer={LayerMask.LayerToName(go.layer)})");

            if (includeComponents)
            {
                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    sb.AppendLine($"{indent}  [Component] {comp.GetType().Name}");
                }
            }

            if (depth < maxChildDepth)
            {
                for (int i = 0; i < go.transform.childCount && i < 30; i++)
                    AppendGameObject(sb, go.transform.GetChild(i).gameObject, depth + 1, includeComponents, maxChildDepth);

                if (go.transform.childCount > 30)
                    sb.AppendLine($"{indent}  ... {go.transform.childCount - 30} more children");
            }
        }
    }
}
