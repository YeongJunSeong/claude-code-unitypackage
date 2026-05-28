namespace ClaudeCode.Editor.Core
{
    public enum ErrorCategory
    {
        Generic,
        CliNotFound,
        NotAuthenticated,
        SessionConflict,
        ProcessExitedEmpty,
        RateLimit,
        Network,
        SandboxBlocked,
        PermissionDenied
    }

    public class ErrorInfo
    {
        public ErrorCategory Category;
        public string UserMessage;
        public string RawMessage;
        public bool Retryable;
        public string ActionLabel;

        public static ErrorInfo Classify(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return new ErrorInfo { Category = ErrorCategory.Generic, UserMessage = "Unknown error.", RawMessage = raw ?? "", Retryable = true };

            var lower = raw.ToLowerInvariant();

            if (lower.Contains("cli not found") || lower.Contains("claude_cli_path"))
                return new ErrorInfo
                {
                    Category = ErrorCategory.CliNotFound,
                    UserMessage = "Claude Code CLI not found. Install it or set CLAUDE_CLI_PATH.",
                    RawMessage = raw,
                    Retryable = false,
                    ActionLabel = "Open Settings"
                };

            if (lower.Contains("not authenticated") || lower.Contains("login required") || lower.Contains("authentication"))
                return new ErrorInfo
                {
                    Category = ErrorCategory.NotAuthenticated,
                    UserMessage = "Not signed in. Sign in via Account menu.",
                    RawMessage = raw,
                    Retryable = false,
                    ActionLabel = "Sign in"
                };

            if (lower.Contains("session id") && lower.Contains("already in use"))
                return new ErrorInfo
                {
                    Category = ErrorCategory.SessionConflict,
                    UserMessage = "Session conflict. Starting a new conversation.",
                    RawMessage = raw,
                    Retryable = true,
                    ActionLabel = "Retry"
                };

            if (lower.Contains("rate limit") || lower.Contains("429") || lower.Contains("too many requests"))
                return new ErrorInfo
                {
                    Category = ErrorCategory.RateLimit,
                    UserMessage = "Rate limited. Please wait a moment and retry.",
                    RawMessage = raw,
                    Retryable = true,
                    ActionLabel = "Retry"
                };

            if (lower.Contains("econnrefused") || lower.Contains("network") || lower.Contains("enotfound") ||
                lower.Contains("etimedout") || lower.Contains("connection reset"))
                return new ErrorInfo
                {
                    Category = ErrorCategory.Network,
                    UserMessage = "Network error connecting to Claude.",
                    RawMessage = raw,
                    Retryable = true,
                    ActionLabel = "Retry"
                };

            if (lower.Contains("sandbox") && (lower.Contains("block") || lower.Contains("denied")))
                return new ErrorInfo
                {
                    Category = ErrorCategory.SandboxBlocked,
                    UserMessage = "Sandbox blocked the request. The path may be outside the working directory.",
                    RawMessage = raw,
                    Retryable = false
                };

            if (lower.Contains("process exited") || lower.Contains("exited unexpectedly"))
                return new ErrorInfo
                {
                    Category = ErrorCategory.ProcessExitedEmpty,
                    UserMessage = "Claude exited without responding. Try again.",
                    RawMessage = raw,
                    Retryable = true,
                    ActionLabel = "Retry"
                };

            return new ErrorInfo
            {
                Category = ErrorCategory.Generic,
                UserMessage = raw.Length > 200 ? raw.Substring(0, 200) + "..." : raw,
                RawMessage = raw,
                Retryable = true,
                ActionLabel = "Retry"
            };
        }
    }
}
