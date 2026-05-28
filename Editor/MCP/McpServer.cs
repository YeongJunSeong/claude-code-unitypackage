using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEditor;
using UnityEngine;
using ClaudeCode.Editor.MCP.Tools;

namespace ClaudeCode.Editor.MCP
{
    public enum PermissionDecision
    {
        Deny = 0,
        AllowOnce = 1,
        AllowForSession = 2,
        AllowAlways = 3
    }

    public class PermissionRequest
    {
        public string ToolName;
        public string ToolInput;
        public System.Collections.Generic.Dictionary<string, object> RawInput;
        public TaskResult<PermissionDecision> Decision;
    }

    public class TaskResult<T>
    {
        readonly ManualResetEventSlim _signal = new ManualResetEventSlim(false);
        T _value;

        public void Set(T value)
        {
            _value = value;
            _signal.Set();
        }

        public T Wait(TimeSpan timeout)
        {
            _signal.Wait(timeout);
            return _value;
        }
    }

    public class McpServer : IDisposable
    {
        HttpListener _listener;
        Thread _listenThread;
        volatile bool _disposed;
        readonly McpToolRegistry _registry;
        readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
        readonly ConcurrentQueue<PermissionRequest> _pendingPermissions = new ConcurrentQueue<PermissionRequest>();

        public int Port { get; private set; }
        public bool IsRunning { get; private set; }

        public event Action<PermissionRequest> OnPermissionRequested;
        public event Action OnFileModificationApproved;

        public McpServer()
        {
            _registry = new McpToolRegistry();
            _registry.Register(new SceneQueryTool());
            _registry.Register(new SceneManipulateTool());
            _registry.Register(new AssetSearchTool());
            _registry.Register(new PermissionPromptTool(this));
            _registry.Register(new StartProfileCaptureTool());
            _registry.Register(new StopProfileCaptureTool());
            _registry.Register(new ProfileStatusTool());
        }

        public void Start(int port = 0)
        {
            if (IsRunning) return;

            try
            {
                if (port == 0) port = FindAvailablePort();

                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();
                Port = port;
                IsRunning = true;

                _listenThread = new Thread(ListenLoop) { IsBackground = true };
                _listenThread.Start();

                EditorApplication.update += ProcessMainThreadQueue;

                Debug.Log($"[ClaudeCode] MCP HTTP server started on port {Port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ClaudeCode] MCP server failed to start: {e.Message}");
            }
        }

        static int FindAvailablePort()
        {
            var sock = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            sock.Start();
            int port = ((IPEndPoint)sock.LocalEndpoint).Port;
            sock.Stop();
            return port;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            _disposed = true;
            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }
            EditorApplication.update -= ProcessMainThreadQueue;
            IsRunning = false;
            Debug.Log("[ClaudeCode] MCP server stopped");
        }

        void ListenLoop()
        {
            while (!_disposed && _listener != null && _listener.IsListening)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    var thread = new Thread(() => HandleRequest(ctx)) { IsBackground = true };
                    thread.Start();
                }
                catch (HttpListenerException) { break; }
                catch (Exception e)
                {
                    if (!_disposed) Debug.LogWarning($"[ClaudeCode] Listen error: {e.Message}");
                }
            }
        }

        void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                if (ctx.Request.HttpMethod != "POST")
                {
                    ctx.Response.StatusCode = 405;
                    ctx.Response.Close();
                    return;
                }

                string body;
                using (var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                    body = reader.ReadToEnd();

                var responseJson = ProcessRequest(body);

                ctx.Response.ContentType = "application/json";
                ctx.Response.Headers.Add("Cache-Control", "no-cache");
                var buf = Encoding.UTF8.GetBytes(responseJson ?? "{}");
                ctx.Response.ContentLength64 = buf.Length;
                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                ctx.Response.OutputStream.Close();
            }
            catch (Exception e)
            {
                if (!_disposed) Debug.LogWarning($"[ClaudeCode] Request error: {e.Message}");
                try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
            }
        }

        string ProcessRequest(string json)
        {
            try
            {
                var request = McpJsonParser.ParseRequest(json);
                if (request == null) return null;

                switch (request.method)
                {
                    case "initialize":
                        return CreateResponse(request.id, new Dictionary<string, object>
                        {
                            ["protocolVersion"] = "2024-11-05",
                            ["capabilities"] = new Dictionary<string, object>
                            {
                                ["tools"] = new Dictionary<string, object>()
                            },
                            ["serverInfo"] = new Dictionary<string, object>
                            {
                                ["name"] = "unity-claude-code",
                                ["version"] = "0.1.0"
                            }
                        });

                    case "notifications/initialized":
                        return null;

                    case "tools/list":
                        return CreateResponse(request.id, new Dictionary<string, object>
                        {
                            ["tools"] = _registry.ListTools()
                        });

                    case "tools/call":
                        return HandleToolCall(request);

                    default:
                        return CreateErrorResponse(request.id, -32601, $"Method not found: {request.method}");
                }
            }
            catch (Exception e)
            {
                return CreateErrorResponse(null, -32700, $"Parse error: {e.Message}");
            }
        }

        string HandleToolCall(JsonRpcRequest request)
        {
            if (request.@params == null)
                return CreateErrorResponse(request.id, -32602, "Missing params");

            var toolName = request.@params.ContainsKey("name") ? request.@params["name"]?.ToString() : "";
            var tool = _registry.GetTool(toolName);
            if (tool == null)
                return CreateErrorResponse(request.id, -32602, $"Unknown tool: {toolName}");

            var args = request.@params.ContainsKey("arguments")
                ? request.@params["arguments"] as Dictionary<string, object>
                : new Dictionary<string, object>();

            string result = null;
            var done = new ManualResetEventSlim(false);
            bool needsMainThread = tool.Name != "permission_prompt";

            if (needsMainThread)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    try { result = tool.Execute(args ?? new Dictionary<string, object>()); }
                    catch (Exception e) { result = $"Error: {e.Message}"; }
                    finally { done.Set(); }
                });
                done.Wait(TimeSpan.FromSeconds(30));
            }
            else
            {
                try { result = tool.Execute(args ?? new Dictionary<string, object>()); }
                catch (Exception e) { result = $"Error: {e.Message}"; }
            }

            return CreateResponse(request.id, new Dictionary<string, object>
            {
                ["content"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "text",
                        ["text"] = result ?? "Execution timed out"
                    }
                }
            });
        }

        internal void RequestPermission(PermissionRequest request)
        {
            _pendingPermissions.Enqueue(request);
            _mainThreadQueue.Enqueue(() =>
            {
                OnPermissionRequested?.Invoke(request);
            });
        }

        internal void NotifyFileModificationApproved()
        {
            _mainThreadQueue.Enqueue(() =>
            {
                OnFileModificationApproved?.Invoke();
            });
        }

        void ProcessMainThreadQueue()
        {
            while (_mainThreadQueue.TryDequeue(out var action))
                action?.Invoke();
        }

        string CreateResponse(string id, object result)
        {
            return McpJsonSerializer.SerializeResponse(id, result, null);
        }

        string CreateErrorResponse(string id, int code, string message)
        {
            return McpJsonSerializer.SerializeResponse(id, null, new JsonRpcError(code, message));
        }

        public void Dispose() => Stop();
    }
}
