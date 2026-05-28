using System;
using System.Collections.Generic;

namespace ClaudeCode.Editor.Core
{
    [Serializable]
    public class StreamEvent
    {
        public string type;
        public string subtype;
        public string session_id;
    }

    [Serializable]
    public class AssistantEvent : StreamEvent
    {
        public AssistantMessage message;
    }

    [Serializable]
    public class AssistantMessage
    {
        public string id;
        public string role;
        public List<ContentBlock> content;
    }

    [Serializable]
    public class ContentBlock
    {
        public string type;
        public string text;
        public string id;
        public string name;
    }

    [Serializable]
    public class UsageInfo
    {
        public int input_tokens;
        public int cache_creation_input_tokens;
        public int cache_read_input_tokens;
        public int output_tokens;

        public int TotalInput => input_tokens + cache_creation_input_tokens + cache_read_input_tokens;
        public int Total => TotalInput + output_tokens;
    }

    [Serializable]
    public class ResultEvent : StreamEvent
    {
        public string result;
        public bool is_error;
        public int duration_ms;
        public UsageInfo usage;
        public double total_cost_usd;
    }

    [Serializable]
    public class SystemEvent : StreamEvent
    {
        public string cwd;
        public string model;
    }

    [Serializable]
    public class StreamEventWrapper : StreamEvent
    {
        public StreamInnerEvent @event;
    }

    [Serializable]
    public class StreamInnerEvent
    {
        public string type;
        public int index;
        public StreamDelta delta;
        public StreamContentBlock content_block;
    }

    [Serializable]
    public class StreamDelta
    {
        public string type;
        public string text;
        public string thinking;
    }

    [Serializable]
    public class StreamContentBlock
    {
        public string type;
        public string text;
        public string id;
        public string name;
    }

    [Serializable]
    public class ToolUseInfo
    {
        public string id;
        public string name;
    }
}
