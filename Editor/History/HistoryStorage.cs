using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ClaudeCode.Editor.Core;

namespace ClaudeCode.Editor.History
{
    [Serializable]
    public class SessionRecord
    {
        public string sessionId;
        public string title;
        public string createdAt;
        public string lastMessageAt;
        public List<ChatMessage> messages = new List<ChatMessage>();

        public string GetDisplayTitle()
        {
            if (!string.IsNullOrEmpty(title)) return title;
            if (messages != null && messages.Count > 0)
            {
                var first = messages[0].content ?? "";
                return first.Length > 60 ? first.Substring(0, 60) + "..." : first;
            }
            return "(empty)";
        }
    }

    public static class HistoryStorage
    {
        static string StorageDirectory
        {
            get
            {
                var dir = Path.Combine(Application.dataPath, "..", "ClaudeCodeHistory");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return Path.GetFullPath(dir);
            }
        }

        public static void Save(string sessionId, List<ChatMessage> messages)
        {
            if (messages == null || messages.Count == 0) return;

            string title = null;
            foreach (var m in messages)
            {
                if (m.role == "user" && !string.IsNullOrWhiteSpace(m.content))
                {
                    title = m.content.Length > 60 ? m.content.Substring(0, 60) + "..." : m.content;
                    break;
                }
            }

            var record = new SessionRecord
            {
                sessionId = sessionId,
                title = title,
                createdAt = messages.Count > 0 ? messages[0].timestamp : DateTime.Now.ToString("O"),
                lastMessageAt = DateTime.Now.ToString("O"),
                messages = messages
            };

            var filePath = Path.Combine(StorageDirectory, $"{sessionId}.json");
            var json = JsonUtility.ToJson(record, true);
            File.WriteAllText(filePath, json);
        }

        public static SessionRecord Load(string sessionId)
        {
            var filePath = Path.Combine(StorageDirectory, $"{sessionId}.json");
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<SessionRecord>(json);
        }

        public static List<SessionRecord> ListSessions()
        {
            var sessions = new List<SessionRecord>();
            var dir = StorageDirectory;

            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var record = JsonUtility.FromJson<SessionRecord>(json);
                    if (record != null)
                        sessions.Add(record);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ClaudeCode] Failed to load session: {e.Message}");
                }
            }

            sessions.Sort((a, b) => string.Compare(b.lastMessageAt, a.lastMessageAt, StringComparison.Ordinal));
            return sessions;
        }

        public static void Delete(string sessionId)
        {
            var filePath = Path.Combine(StorageDirectory, $"{sessionId}.json");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
