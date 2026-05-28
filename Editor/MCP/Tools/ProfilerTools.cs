using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ClaudeCode.Editor.MCP.Tools
{
    public class StartProfileCaptureTool : IMcpTool
    {
        public string Name => "unity_profile_start";
        public string Description =>
            "Start a Unity profiler capture session. Records CPU times (PlayerLoop, Update, Physics, Render), memory usage, GC allocations, and render stats (draw calls, batches, triangles). " +
            "Capture continues until unity_profile_stop is called. Should be called while Play Mode is running for meaningful CPU/render samples. " +
            "Only one active capture allowed; calling start while active restarts the session.";

        public McpInputSchema InputSchema => new McpInputSchema
        {
            properties = new Dictionary<string, McpPropertyDef>(),
            required = new List<string>()
        };

        public string Execute(Dictionary<string, object> args)
        {
            var sb = new StringBuilder();
            bool wasPlaying = EditorApplication.isPlaying;

            ProfilerSession.Start();

            sb.AppendLine($"Profile capture started at {ProfilerSession.StartedAt:HH:mm:ss}.");
            sb.AppendLine($"Play Mode: {(wasPlaying ? "running" : "NOT running — only memory/render markers will be meaningful")}");
            sb.AppendLine("Capacity: ~60 seconds at 60fps. Beyond that, oldest samples are discarded.");
            sb.AppendLine("Call unity_profile_stop to stop and retrieve aggregated stats.");
            return sb.ToString();
        }
    }

    public class StopProfileCaptureTool : IMcpTool
    {
        public string Name => "unity_profile_stop";
        public string Description =>
            "Stop the active profiler capture and return aggregated statistics: avg/min/max/p95 of CPU times, memory, and render counts. " +
            "Returns an error if no capture is active.";

        public McpInputSchema InputSchema => new McpInputSchema
        {
            properties = new Dictionary<string, McpPropertyDef>(),
            required = new List<string>()
        };

        public string Execute(Dictionary<string, object> args)
        {
            if (!ProfilerSession.IsActive)
            {
                if (ProfilerSession.LastResult != null)
                {
                    var ago = (System.DateTime.UtcNow - ProfilerSession.LastResultAt).TotalSeconds;
                    return $"(No active capture; returning last result from {ago:F0}s ago)\n\n"
                        + ProfilerSession.FormatResultAsText(ProfilerSession.LastResult);
                }
                return "No active profile capture and no cached result. Call unity_profile_start first.";
            }

            var result = ProfilerSession.Stop();
            return ProfilerSession.FormatResultAsText(result);
        }
    }

    public class ProfileStatusTool : IMcpTool
    {
        public string Name => "unity_profile_status";
        public string Description =>
            "Check whether a profiler capture is currently active. Returns active state, elapsed seconds since capture started, and whether Play Mode is running.";

        public McpInputSchema InputSchema => new McpInputSchema
        {
            properties = new Dictionary<string, McpPropertyDef>(),
            required = new List<string>()
        };

        public string Execute(Dictionary<string, object> args)
        {
            if (!ProfilerSession.IsActive)
                return $"No active capture. Play Mode: {(EditorApplication.isPlaying ? "running" : "stopped")}.";

            return $"Profile capture active. Elapsed: {ProfilerSession.ElapsedSeconds:F1}s. Play Mode: {(EditorApplication.isPlaying ? "running" : "stopped")}.";
        }
    }
}
