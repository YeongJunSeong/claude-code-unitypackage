using System.Collections.Generic;

namespace ClaudeCode.Editor.Approval
{
    /// <summary>
    /// In-memory cache of tool names auto-allowed for the current chat session.
    /// Cleared whenever a new session begins.
    /// </summary>
    public static class SessionPermissionCache
    {
        static readonly HashSet<string> _allowed = new HashSet<string>();

        public static bool IsAllowed(string toolName)
        {
            if (string.IsNullOrEmpty(toolName)) return false;
            return _allowed.Contains(toolName);
        }

        public static void Allow(string toolName)
        {
            if (!string.IsNullOrEmpty(toolName))
                _allowed.Add(toolName);
        }

        public static void Clear() => _allowed.Clear();
    }
}
