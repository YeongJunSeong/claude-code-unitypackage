using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace ClaudeCode.Editor.MCP.Tools
{
    public class SceneManipulateTool : IMcpTool
    {
        public string Name => "unity_scene_manipulate";
        public string Description => "Create, modify, or delete GameObjects in the active Unity scene. Actions: create, delete, rename, set_active, set_position, add_component.";

        public McpInputSchema InputSchema => new McpInputSchema
        {
            properties = new Dictionary<string, McpPropertyDef>
            {
                ["action"] = new McpPropertyDef { type = "string", description = "Action: create, delete, rename, set_active, set_position, add_component" },
                ["target"] = new McpPropertyDef { type = "string", description = "Name of the target GameObject (for existing objects)" },
                ["name"] = new McpPropertyDef { type = "string", description = "New name (for create/rename)" },
                ["value"] = new McpPropertyDef { type = "string", description = "Value for the action (e.g., 'true'/'false' for set_active, 'x,y,z' for set_position, component type for add_component)" }
            },
            required = new List<string> { "action" }
        };

        public string Execute(Dictionary<string, object> args)
        {
            var action = args.ContainsKey("action") ? args["action"]?.ToString() : "";
            var target = args.ContainsKey("target") ? args["target"]?.ToString() : "";
            var name = args.ContainsKey("name") ? args["name"]?.ToString() : "";
            var value = args.ContainsKey("value") ? args["value"]?.ToString() : "";

            switch (action)
            {
                case "create":
                    return CreateObject(name);
                case "delete":
                    return DeleteObject(target);
                case "rename":
                    return RenameObject(target, name);
                case "set_active":
                    return SetActive(target, value);
                case "set_position":
                    return SetPosition(target, value);
                case "add_component":
                    return AddComponent(target, value);
                default:
                    return $"Unknown action: {action}";
            }
        }

        string CreateObject(string name)
        {
            var go = new GameObject(string.IsNullOrEmpty(name) ? "New GameObject" : name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");
            return $"Created: {go.name}";
        }

        string DeleteObject(string target)
        {
            var go = GameObject.Find(target);
            if (go == null) return $"Not found: {target}";
            Undo.DestroyObjectImmediate(go);
            return $"Deleted: {target}";
        }

        string RenameObject(string target, string newName)
        {
            var go = GameObject.Find(target);
            if (go == null) return $"Not found: {target}";
            Undo.RecordObject(go, $"Rename {target}");
            go.name = newName;
            return $"Renamed: {target} -> {newName}";
        }

        string SetActive(string target, string value)
        {
            var go = GameObject.Find(target);
            if (go == null) return $"Not found: {target}";
            Undo.RecordObject(go, $"SetActive {target}");
            go.SetActive(value == "true");
            return $"Set active={value}: {target}";
        }

        string SetPosition(string target, string value)
        {
            var go = GameObject.Find(target);
            if (go == null) return $"Not found: {target}";

            var parts = value.Split(',');
            if (parts.Length != 3) return "Invalid position format. Use: x,y,z";

            if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
                !float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                return "Invalid position values.";

            Undo.RecordObject(go.transform, $"Move {target}");
            go.transform.position = new Vector3(x, y, z);
            return $"Moved {target} to ({x}, {y}, {z})";
        }

        string AddComponent(string target, string componentType)
        {
            var go = GameObject.Find(target);
            if (go == null) return $"Not found: {target}";

            var type = System.Type.GetType($"UnityEngine.{componentType}, UnityEngine")
                       ?? System.Type.GetType(componentType);
            if (type == null) return $"Unknown component type: {componentType}";

            Undo.AddComponent(go, type);
            return $"Added {componentType} to {target}";
        }
    }
}
