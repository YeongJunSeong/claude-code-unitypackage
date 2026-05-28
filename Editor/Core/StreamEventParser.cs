using System;
using UnityEngine;

namespace ClaudeCode.Editor.Core
{
    public static class StreamEventParser
    {
        public static StreamEvent Parse(string jsonLine)
        {
            if (string.IsNullOrWhiteSpace(jsonLine))
                return null;

            try
            {
                var baseEvent = JsonUtility.FromJson<StreamEvent>(jsonLine);
                if (baseEvent == null || string.IsNullOrEmpty(baseEvent.type))
                    return null;

                return baseEvent.type switch
                {
                    "stream_event" => JsonUtility.FromJson<StreamEventWrapper>(jsonLine),
                    "assistant" => JsonUtility.FromJson<AssistantEvent>(jsonLine),
                    "result" => JsonUtility.FromJson<ResultEvent>(jsonLine),
                    "system" => JsonUtility.FromJson<SystemEvent>(jsonLine),
                    _ => baseEvent
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ClaudeCode] Failed to parse stream event: {e.Message}");
                return null;
            }
        }
    }
}
