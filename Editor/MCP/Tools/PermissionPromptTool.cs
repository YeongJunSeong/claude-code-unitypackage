using System;
using System.Collections.Generic;
using ClaudeCode.Editor.Approval;

namespace ClaudeCode.Editor.MCP.Tools
{
    public class PermissionPromptTool : IMcpTool
    {
        readonly McpServer _server;

        public PermissionPromptTool(McpServer server)
        {
            _server = server;
        }

        public string Name => "permission_prompt";
        public string Description => "Request user permission to use a tool. Called by Claude Code before executing tools when --permission-prompt-tool is set.";

        public McpInputSchema InputSchema => new McpInputSchema
        {
            properties = new Dictionary<string, McpPropertyDef>
            {
                ["tool_name"] = new McpPropertyDef { type = "string", description = "Name of the tool being requested" },
                ["input"] = new McpPropertyDef { type = "object", description = "Input arguments to the tool" },
                ["tool_use_id"] = new McpPropertyDef { type = "string", description = "Unique identifier for this tool use" }
            },
            required = new List<string> { "tool_name", "input" }
        };

        public string Execute(Dictionary<string, object> args)
        {
            var toolName = args.TryGetValue("tool_name", out var n) ? n?.ToString() : "unknown";
            args.TryGetValue("input", out var rawInput);
            var inputDict = rawInput as Dictionary<string, object> ?? new Dictionary<string, object>();
            var inputJsonForUi = McpJsonSerializer.Serialize(inputDict);

            // 패키지 자기수정 차단: UPM(immutable)으로 설치된 경우, 패키지 자신의
            // 파일을 수정/복사/이동/삭제하려는 시도는 다른 모든 규칙보다 우선해서 무조건 거부.
            if (PackageSelfGuard.ShouldBlock(toolName, inputDict, out var blockedPath))
                return BuildDenyResponse(
                    "Claude Code 패키지(com.dnsoft.claudecode)는 읽기 전용 UPM 패키지로 설치되어 " +
                    "있어 수정/복사/이동/삭제할 수 없습니다. 패키지를 프로젝트로 복사해서 우회하려는 " +
                    "시도도 허용되지 않습니다. 패키지 코드 변경이 필요하면 사용자에게 알리고 중단하세요. " +
                    $"차단된 작업: {blockedPath}");

            var mode = PermissionModeManager.Current;

            // AcceptEdits 모드: 편집 도구는 자동 승인
            if (mode == PermissionMode.AcceptEdits && IsEditTool(toolName))
                return AllowAndNotify(toolName, inputDict);

            // PlanMode: 모든 편집 도구 차단
            if (mode == PermissionMode.PlanMode && IsEditTool(toolName))
                return BuildDenyResponse("계획 모드에서는 파일/씬을 수정할 수 없습니다.");

            // 세션 동안 허용 캐시 체크
            if (SessionPermissionCache.IsAllowed(toolName))
                return AllowAndNotify(toolName, inputDict);

            var request = new PermissionRequest
            {
                ToolName = toolName,
                ToolInput = inputJsonForUi,
                RawInput = inputDict,
                Decision = new TaskResult<PermissionDecision>()
            };

            _server.RequestPermission(request);

            var decision = request.Decision.Wait(TimeSpan.FromMinutes(5));

            switch (decision)
            {
                case PermissionDecision.AllowOnce:
                    return AllowAndNotify(toolName, inputDict);

                case PermissionDecision.AllowForSession:
                    SessionPermissionCache.Allow(toolName);
                    return AllowAndNotify(toolName, inputDict);

                case PermissionDecision.AllowAlways:
                    return AllowAndNotifyWithRule(toolName, inputDict);

                case PermissionDecision.Deny:
                default:
                    return BuildDenyResponse("Denied by user.");
            }
        }

        string AllowAndNotify(string toolName, Dictionary<string, object> input)
        {
            if (IsFileWritingTool(toolName))
                _server.NotifyFileModificationApproved();
            return BuildAllowResponse(input);
        }

        string AllowAndNotifyWithRule(string toolName, Dictionary<string, object> input)
        {
            if (IsFileWritingTool(toolName))
                _server.NotifyFileModificationApproved();
            return BuildAllowResponseWithRule(input, toolName);
        }

        static bool IsEditTool(string toolName)
        {
            if (string.IsNullOrEmpty(toolName)) return false;
            switch (toolName)
            {
                case "Write":
                case "Edit":
                case "MultiEdit":
                case "NotebookEdit":
                case "unity_scene_manipulate":
                    return true;
                default:
                    return false;
            }
        }

        static bool IsFileWritingTool(string toolName)
        {
            if (string.IsNullOrEmpty(toolName)) return false;
            switch (toolName)
            {
                case "Write":
                case "Edit":
                case "MultiEdit":
                case "NotebookEdit":
                    return true;
                default:
                    return false;
            }
        }

        static string BuildAllowResponse(Dictionary<string, object> updatedInput)
        {
            var response = new Dictionary<string, object>
            {
                ["behavior"] = "allow",
                ["updatedInput"] = updatedInput
            };
            return McpJsonSerializer.Serialize(response);
        }

        static string BuildAllowResponseWithRule(Dictionary<string, object> updatedInput, string toolName)
        {
            var response = new Dictionary<string, object>
            {
                ["behavior"] = "allow",
                ["updatedInput"] = updatedInput,
                ["permissionRule"] = toolName
            };
            return McpJsonSerializer.Serialize(response);
        }

        static string BuildDenyResponse(string message)
        {
            var response = new Dictionary<string, object>
            {
                ["behavior"] = "deny",
                ["message"] = message
            };
            return McpJsonSerializer.Serialize(response);
        }
    }
}
