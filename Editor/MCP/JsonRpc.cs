using System;
using System.Collections.Generic;

namespace ClaudeCode.Editor.MCP
{
    [Serializable]
    public class JsonRpcRequest
    {
        public string jsonrpc = "2.0";
        public string method;
        public string id;
        public Dictionary<string, object> @params;
    }

    [Serializable]
    public class JsonRpcResponse
    {
        public string jsonrpc = "2.0";
        public string id;
        public object result;
        public JsonRpcError error;
    }

    [Serializable]
    public class JsonRpcError
    {
        public int code;
        public string message;

        public JsonRpcError(int code, string message)
        {
            this.code = code;
            this.message = message;
        }
    }

    [Serializable]
    public class McpToolDefinition
    {
        public string name;
        public string description;
        public McpInputSchema inputSchema;
    }

    [Serializable]
    public class McpInputSchema
    {
        public string type = "object";
        public Dictionary<string, McpPropertyDef> properties;
        public List<string> required;
    }

    [Serializable]
    public class McpPropertyDef
    {
        public string type;
        public string description;
    }
}
