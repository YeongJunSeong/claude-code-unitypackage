using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ClaudeCode.Editor.History;

namespace ClaudeCode.Editor.Core
{
    [FilePath("ClaudeCode/SessionState.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class SessionSerializer : ScriptableSingleton<SessionSerializer>
    {
        [SerializeField] string _activeSessionId;
        [SerializeField] string _serializedMessages;

        public void SaveState(SessionManager session)
        {
            if (session == null) return;

            _activeSessionId = session.SessionId;
            _serializedMessages = JsonUtility.ToJson(new MessageList { messages = session.Messages });
            Save(true);

            HistoryStorage.Save(session.SessionId, session.Messages);
        }

        public void RestoreState(SessionManager session)
        {
            if (string.IsNullOrEmpty(_activeSessionId)) return;

            var record = HistoryStorage.Load(_activeSessionId);
            if (record == null) return;

            session.Messages.Clear();
            session.Messages.AddRange(record.messages);
        }

        public string GetActiveSessionId() => _activeSessionId;

        public void ClearState()
        {
            _activeSessionId = null;
            _serializedMessages = null;
            Save(true);
        }

        [Serializable]
        class MessageList
        {
            public List<ChatMessage> messages;
        }
    }
}
