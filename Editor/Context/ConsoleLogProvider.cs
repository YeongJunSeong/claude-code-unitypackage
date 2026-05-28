using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ClaudeCode.Editor.Context
{
    [InitializeOnLoad]
    static class ConsoleLogProviderBootstrap
    {
        static ConsoleLogProviderBootstrap()
        {
            ConsoleLogProvider.StartListening();
        }
    }

    public class ConsoleError
    {
        public string Message;
        public string StackTrace;
        public LogType Type;
        public DateTime Timestamp;
        public string FilePath;
        public int LineNumber;
        public int Id;

        public string ShortLocation
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath)) return "";
                string name;
                try { name = System.IO.Path.GetFileName(FilePath); }
                catch
                {
                    // FilePath may contain invalid path characters when parsed loosely from stack
                    // traces; fall back to a manual split.
                    var p = FilePath.Replace('\\', '/');
                    int idx = p.LastIndexOf('/');
                    name = idx >= 0 ? p.Substring(idx + 1) : p;
                }
                return LineNumber > 0 ? $"{name}:{LineNumber}" : name;
            }
        }
    }

    public static class ConsoleLogProvider
    {
        static readonly List<LogEntry> _recentLogs = new List<LogEntry>();
        static readonly List<ConsoleError> _errors = new List<ConsoleError>();
        const int MaxLogs = 50;
        const int MaxErrors = 30;
        static bool _listening;
        static int _nextErrorId;

        public static event Action OnErrorsChanged;

        public static IReadOnlyList<ConsoleError> Errors => _errors;
        public static int ErrorCount => _errors.Count;
        public static ConsoleError LatestError => _errors.Count > 0 ? _errors[_errors.Count - 1] : null;

        struct LogEntry
        {
            public string message;
            public LogType type;
            public string timestamp;
        }

        static readonly Regex StackLocationRegex = new Regex(
            @"\(at (.+?):(\d+)\)", RegexOptions.Compiled);

        public static void StartListening()
        {
            if (_listening) return;
            Application.logMessageReceived += OnLogMessage;
            _listening = true;
        }

        public static void StopListening()
        {
            Application.logMessageReceived -= OnLogMessage;
            _listening = false;
        }

        static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            _recentLogs.Add(new LogEntry
            {
                message = message,
                type = type,
                timestamp = DateTime.Now.ToString("HH:mm:ss")
            });
            if (_recentLogs.Count > MaxLogs) _recentLogs.RemoveAt(0);

            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                var err = new ConsoleError
                {
                    Id = ++_nextErrorId,
                    Message = message ?? "",
                    StackTrace = stackTrace ?? "",
                    Type = type,
                    Timestamp = DateTime.Now
                };
                ParseLocation(stackTrace, err);

                _errors.Add(err);
                if (_errors.Count > MaxErrors) _errors.RemoveAt(0);

                try { OnErrorsChanged?.Invoke(); } catch { }
            }
        }

        static void ParseLocation(string stackTrace, ConsoleError err)
        {
            if (string.IsNullOrEmpty(stackTrace)) return;
            foreach (Match m in StackLocationRegex.Matches(stackTrace))
            {
                var path = m.Groups[1].Value;
                if (path.StartsWith("Assets/") || path.StartsWith("Packages/"))
                {
                    err.FilePath = path;
                    int.TryParse(m.Groups[2].Value, out err.LineNumber);
                    return;
                }
            }

            var first = StackLocationRegex.Match(stackTrace);
            if (first.Success)
            {
                err.FilePath = first.Groups[1].Value;
                int.TryParse(first.Groups[2].Value, out err.LineNumber);
            }
        }

        public static string GetRecentErrors(int count = 10)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Console Errors]");

            int start = Mathf.Max(0, _errors.Count - count);
            for (int i = _errors.Count - 1; i >= start; i--)
            {
                var e = _errors[i];
                sb.AppendLine($"  [{e.Timestamp:HH:mm:ss}] {e.Type}: {e.Message}");
                if (!string.IsNullOrEmpty(e.ShortLocation))
                    sb.AppendLine($"    at {e.ShortLocation}");
            }

            if (_errors.Count == 0)
                sb.AppendLine("  No recent errors.");

            return sb.ToString();
        }

        public static string GetRecentLogs(int count = 20)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Console Logs]");

            int start = Mathf.Max(0, _recentLogs.Count - count);
            for (int i = start; i < _recentLogs.Count; i++)
            {
                var log = _recentLogs[i];
                sb.AppendLine($"  [{log.timestamp}] {log.type}: {log.message}");
            }

            if (_recentLogs.Count == 0)
                sb.AppendLine("  No recent logs.");

            return sb.ToString();
        }

        public static void ClearLogs() => _recentLogs.Clear();

        public static void ClearErrors()
        {
            _errors.Clear();
            try { OnErrorsChanged?.Invoke(); } catch { }
        }

        public static void RemoveError(ConsoleError err)
        {
            if (_errors.Remove(err))
                try { OnErrorsChanged?.Invoke(); } catch { }
        }
    }
}
