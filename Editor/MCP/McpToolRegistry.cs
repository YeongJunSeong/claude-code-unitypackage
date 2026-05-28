using System;
using System.Collections.Generic;

namespace ClaudeCode.Editor.MCP
{
    public interface IMcpTool
    {
        string Name { get; }
        string Description { get; }
        McpInputSchema InputSchema { get; }
        string Execute(Dictionary<string, object> args);
    }

    public class McpToolRegistry
    {
        readonly Dictionary<string, IMcpTool> _tools = new Dictionary<string, IMcpTool>();

        public void Register(IMcpTool tool)
        {
            _tools[tool.Name] = tool;
        }

        public IMcpTool GetTool(string name)
        {
            _tools.TryGetValue(name, out var tool);
            return tool;
        }

        public List<McpToolDefinition> ListTools()
        {
            var list = new List<McpToolDefinition>();
            foreach (var tool in _tools.Values)
            {
                list.Add(new McpToolDefinition
                {
                    name = tool.Name,
                    description = tool.Description,
                    inputSchema = tool.InputSchema
                });
            }
            return list;
        }
    }
}
