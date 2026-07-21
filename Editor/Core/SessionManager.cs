using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ClaudeCode.Editor.Context;
using ClaudeCode.Editor.Approval;
using ClaudeCode.Editor.MCP;

namespace ClaudeCode.Editor.Core
{
    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
        public string timestamp;

        // Per-turn usage (assistant messages only). Populated on completion.
        public bool hasUsage;
        public int inputTokens;
        public int cacheCreationTokens;
        public int cacheReadTokens;
        public int outputTokens;
        public double costUsd;
    }

    public class SessionManager : IDisposable
    {
        readonly AuthManager _authManager;
        ClaudeProcess _process;
        string _sessionId;
        string _currentAssistantMessage;
        bool _isFirstMessage = true;

        public string McpConfigPath { get; set; }

        public List<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();
        public bool IsProcessing => _process?.State == ProcessState.Running;
        public string SessionId => _sessionId;

        public event Action<string> OnTextDelta;
        public event Action<ChatMessage> OnMessageComplete;
        public event Action<ToolUseInfo> OnToolUse;
        public event Action OnUsageUpdated;

        public UsageInfo LatestUsage { get; private set; }
        public int LatestContextWindow { get; private set; } = 200000;
        public double TotalCostUsd { get; private set; }

        // Holds the usage/cost for the in-flight turn so it can be attached to the
        // assistant ChatMessage when it finalizes.
        UsageInfo _pendingTurnUsage;
        double _pendingTurnCost;

        static readonly System.Text.RegularExpressions.Regex ContextWindowRegex
            = new System.Text.RegularExpressions.Regex(@"""contextWindow""\s*:\s*(\d+)",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        static readonly System.Text.RegularExpressions.Regex InputTokensRegex
            = new System.Text.RegularExpressions.Regex(@"""input_tokens""\s*:\s*(\d+)",
                System.Text.RegularExpressions.RegexOptions.Compiled);
        static readonly System.Text.RegularExpressions.Regex CacheCreationRegex
            = new System.Text.RegularExpressions.Regex(@"""cache_creation_input_tokens""\s*:\s*(\d+)",
                System.Text.RegularExpressions.RegexOptions.Compiled);
        static readonly System.Text.RegularExpressions.Regex CacheReadRegex
            = new System.Text.RegularExpressions.Regex(@"""cache_read_input_tokens""\s*:\s*(\d+)",
                System.Text.RegularExpressions.RegexOptions.Compiled);
        static readonly System.Text.RegularExpressions.Regex OutputTokensRegex
            = new System.Text.RegularExpressions.Regex(@"""output_tokens""\s*:\s*(\d+)",
                System.Text.RegularExpressions.RegexOptions.Compiled);
        static readonly System.Text.RegularExpressions.Regex TotalCostRegex
            = new System.Text.RegularExpressions.Regex(@"""total_cost_usd""\s*:\s*([\d.eE+-]+)",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        static int ExtractIntFromLine(string line, System.Text.RegularExpressions.Regex regex)
        {
            var m = regex.Match(line);
            if (!m.Success) return -1;
            return int.TryParse(m.Groups[1].Value, out var v) ? v : -1;
        }

        static double ExtractDoubleFromLine(string line, System.Text.RegularExpressions.Regex regex)
        {
            var m = regex.Match(line);
            if (!m.Success) return 0;
            return double.TryParse(m.Groups[1].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var v) ? v : 0;
        }
        public event Action<string> OnError;
        public event Action<ErrorInfo> OnDetailedError;

        string _lastUserMessage;
        public string LastUserMessage => _lastUserMessage;

        public SessionManager(AuthManager authManager)
        {
            _authManager = authManager;
            _sessionId = Guid.NewGuid().ToString();
            ConsoleLogProvider.StartListening();
        }

        public bool SendMessage(string userMessage)
        {
            if (IsProcessing) return false;

            _lastUserMessage = userMessage;

            var cliPath = CliLocator.FindClaudeCli();
            if (string.IsNullOrEmpty(cliPath))
            {
                EmitError("cli not found");
                return false;
            }

            if (!_authManager.IsAuthenticated())
            {
                EmitError("not authenticated");
                return false;
            }

            Messages.Add(new ChatMessage
            {
                role = "user",
                content = userMessage,
                timestamp = DateTime.Now.ToString("O")
            });

            _currentAssistantMessage = "";

            var contextPrefix = _isFirstMessage ? ContextCollector.CollectAll() : ContextCollector.CollectMinimal();
            var promptWithContext = $"{contextPrefix}\n\n---\nUser message:\n{userMessage}";
            bool isFirst = _isFirstMessage;
            _isFirstMessage = false;

            var projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
            _process = new ClaudeProcess(cliPath, projectRoot, _authManager);
            _process.OnOutputLine += HandleOutputLine;
            _process.OnErrorLine += HandleErrorLine;
            _process.OnProcessExited += HandleProcessExited;

            EditorApplication.update += Update;

            var extraArgs = BuildExtraArgs();
            return _process.Start(promptWithContext, _sessionId, resume: !isFirst, extraArgs);
        }

        string[] BuildExtraArgs()
        {
            var list = new List<string>();

            var model = ModelManager.CurrentModel;
            if (!string.IsNullOrEmpty(model) && model != "default")
                list.Add($"--model {model}");

            if (!EffortManager.IsDefault)
                list.Add($"--effort {EffortManager.Current}");

            if (!string.IsNullOrEmpty(McpConfigPath))
                list.Add($"--mcp-config \"{McpConfigPath}\"");

            var mode = PermissionModeManager.Current;
            list.Add($"--permission-mode {PermissionModeManager.CliFlag(mode)}");

            if (!string.IsNullOrEmpty(McpConfigPath) && mode != PermissionMode.PlanMode)
                list.Add($"--permission-prompt-tool {McpConfigWriter.PermissionToolFullName}");

            return list.ToArray();
        }

        void Update()
        {
            _process?.ProcessQueue();
        }

        public void Tick()
        {
            _process?.ProcessQueue();
        }

        void HandleOutputLine(string line)
        {
            var evt = StreamEventParser.Parse(line);
            if (evt == null) return;

            switch (evt)
            {
                case StreamEventWrapper wrapper when wrapper.@event != null:
                    HandleStreamEvent(wrapper.@event);
                    break;

                case AssistantEvent assistant when assistant.message?.content != null:
                    foreach (var block in assistant.message.content)
                    {
                        if (block.type == "tool_use")
                            OnToolUse?.Invoke(new ToolUseInfo { id = block.id, name = block.name });
                    }
                    break;

                case ResultEvent result:
                    {
                        int inTokens = ExtractIntFromLine(line, InputTokensRegex);
                        int cacheCreation = ExtractIntFromLine(line, CacheCreationRegex);
                        int cacheRead = ExtractIntFromLine(line, CacheReadRegex);
                        int outTokens = ExtractIntFromLine(line, OutputTokensRegex);

                        if (inTokens >= 0 || cacheCreation >= 0 || cacheRead >= 0)
                        {
                            LatestUsage = new UsageInfo
                            {
                                input_tokens = System.Math.Max(0, inTokens),
                                cache_creation_input_tokens = System.Math.Max(0, cacheCreation),
                                cache_read_input_tokens = System.Math.Max(0, cacheRead),
                                output_tokens = System.Math.Max(0, outTokens)
                            };
                            var cost = ExtractDoubleFromLine(line, TotalCostRegex);
                            if (cost > 0) TotalCostUsd += cost;
                            var cw = ExtractIntFromLine(line, ContextWindowRegex);
                            if (cw > 0) LatestContextWindow = cw;

                            // Stash this turn's usage/cost for the assistant message footer.
                            _pendingTurnUsage = LatestUsage;
                            _pendingTurnCost = cost > 0 ? cost : 0;

                            OnUsageUpdated?.Invoke();
                        }
                    }

                    if (result.is_error)
                    {
                        EmitError(result.result);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(_currentAssistantMessage) && !string.IsNullOrEmpty(result.result))
                            _currentAssistantMessage = result.result;
                        FinalizeAssistantMessage();
                    }
                    break;
            }
        }

        void HandleStreamEvent(StreamInnerEvent inner)
        {
            switch (inner.type)
            {
                case "content_block_delta":
                    if (inner.delta?.type == "text_delta" && !string.IsNullOrEmpty(inner.delta.text))
                    {
                        _currentAssistantMessage += inner.delta.text;
                        OnTextDelta?.Invoke(inner.delta.text);
                    }
                    break;

                case "content_block_start":
                    if (inner.content_block?.type == "tool_use")
                        OnToolUse?.Invoke(new ToolUseInfo { id = inner.content_block.id, name = inner.content_block.name });
                    break;
            }
        }

        void HandleErrorLine(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return;
            EmitError(error);
        }

        void HandleProcessExited()
        {
            bool gotAnyResponse = !string.IsNullOrEmpty(_currentAssistantMessage);
            bool hadAssistantMessage = Messages.Count > 0 && Messages[Messages.Count - 1].role == "assistant";

            if (gotAnyResponse)
                FinalizeAssistantMessage();

            EditorApplication.update -= Update;

            _process?.Dispose();
            _process = null;

            if (!gotAnyResponse && !hadAssistantMessage)
                EmitError("Claude process exited without producing a response.");
        }

        void EmitError(string raw)
        {
            var info = ErrorInfo.Classify(raw);
            OnDetailedError?.Invoke(info);
            OnError?.Invoke(info.UserMessage);
        }

        void FinalizeAssistantMessage()
        {
            if (string.IsNullOrEmpty(_currentAssistantMessage)) return;

            var msg = new ChatMessage
            {
                role = "assistant",
                content = _currentAssistantMessage,
                timestamp = DateTime.Now.ToString("O")
            };
            if (_pendingTurnUsage != null)
            {
                msg.hasUsage = true;
                msg.inputTokens = _pendingTurnUsage.input_tokens;
                msg.cacheCreationTokens = _pendingTurnUsage.cache_creation_input_tokens;
                msg.cacheReadTokens = _pendingTurnUsage.cache_read_input_tokens;
                msg.outputTokens = _pendingTurnUsage.output_tokens;
                msg.costUsd = _pendingTurnCost;
            }
            Messages.Add(msg);
            OnMessageComplete?.Invoke(msg);
            _currentAssistantMessage = "";
            _pendingTurnUsage = null;
            _pendingTurnCost = 0;
        }

        public void StopGeneration()
        {
            _process?.Kill();
        }

        /// <summary>
        /// Removes messages starting from the given index (inclusive).
        /// Used to "rewind" before regeneration or edit-and-resend.
        /// </summary>
        public void TruncateFromIndex(int index)
        {
            if (index < 0 || index >= Messages.Count) return;
            Messages.RemoveRange(index, Messages.Count - index);
        }

        /// <summary>
        /// Finds the index of the last user message, returns -1 if none.
        /// </summary>
        public int FindLastUserMessageIndex()
        {
            for (int i = Messages.Count - 1; i >= 0; i--)
                if (Messages[i].role == "user") return i;
            return -1;
        }

        public void ClearHistory()
        {
            Messages.Clear();
            _sessionId = Guid.NewGuid().ToString();
            _isFirstMessage = true;
            SessionPermissionCache.Clear();
            LatestUsage = null;
            TotalCostUsd = 0;
            OnUsageUpdated?.Invoke();
        }

        public void LoadFromRecord(History.SessionRecord record)
        {
            if (record == null) return;
            Messages.Clear();
            Messages.AddRange(record.messages);
            _sessionId = record.sessionId;
            _isFirstMessage = false;
        }

        public void Dispose()
        {
            EditorApplication.update -= Update;
            ConsoleLogProvider.StopListening();
            _process?.Dispose();
        }
    }
}
