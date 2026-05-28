using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace ClaudeCode.Editor.MCP.Tools
{
    /// <summary>
    /// Manages an active ProfilerRecorder capture session. Single global session
    /// (you don't typically want overlapping profiles). Designed to be driven
    /// by MCP tools (start/stop) or slash commands (/profile start/stop).
    /// </summary>
    public static class ProfilerSession
    {
        struct Tracked
        {
            public ProfilerRecorder Recorder;
            public string Label;
            public ProfilerCategory Category;
            public string Marker;
            public ProfilerMarkerDataUnit Unit;
        }

        static readonly List<Tracked> _recorders = new List<Tracked>();
        static DateTime _startedAt;
        static bool _active;
        const int Capacity = 3600; // ~60s @ 60fps

        public static bool IsActive => _active;
        public static DateTime StartedAt => _startedAt;
        public static double ElapsedSeconds => _active ? (DateTime.UtcNow - _startedAt).TotalSeconds : 0;
        public static CaptureResult LastResult { get; private set; }
        public static DateTime LastResultAt { get; private set; }

        public static event Action OnStateChanged;

        public static void Start()
        {
            if (_active) Stop(silent: true);

            _recorders.Clear();

            // CPU time markers — Value is in nanoseconds
            AddTime("PlayerLoop",             ProfilerCategory.Internal, "PlayerLoop");
            AddTime("Update",                 ProfilerCategory.Scripts,  "BehaviourUpdate");
            AddTime("LateUpdate",             ProfilerCategory.Scripts,  "BehaviourLateUpdate");
            AddTime("FixedUpdate",            ProfilerCategory.Scripts,  "BehaviourFixedUpdate");
            AddTime("Physics.Simulate",       ProfilerCategory.Physics,  "Physics.Simulate");
            AddTime("Camera.Render",          ProfilerCategory.Render,   "Camera.Render");

            // Memory — Value is in bytes
            AddCount("System Used Memory",    ProfilerCategory.Memory,   "System Used Memory",     ProfilerMarkerDataUnit.Bytes);
            AddCount("GC Used Memory",        ProfilerCategory.Memory,   "GC Used Memory",         ProfilerMarkerDataUnit.Bytes);
            AddCount("GC Allocated In Frame", ProfilerCategory.Memory,   "GC Allocated In Frame",  ProfilerMarkerDataUnit.Bytes);

            // Render — Value is a count
            AddCount("Batches",               ProfilerCategory.Render,   "Batches Count",          ProfilerMarkerDataUnit.Count);
            AddCount("Draw Calls",            ProfilerCategory.Render,   "Draw Calls Count",       ProfilerMarkerDataUnit.Count);
            AddCount("SetPass Calls",         ProfilerCategory.Render,   "SetPass Calls Count",    ProfilerMarkerDataUnit.Count);
            AddCount("Triangles",             ProfilerCategory.Render,   "Triangles Count",        ProfilerMarkerDataUnit.Count);
            AddCount("Vertices",              ProfilerCategory.Render,   "Vertices Count",         ProfilerMarkerDataUnit.Count);

            _startedAt = DateTime.UtcNow;
            _active = true;
            OnStateChanged?.Invoke();
        }

        static void AddTime(string label, ProfilerCategory cat, string marker)
        {
            try
            {
                var rec = ProfilerRecorder.StartNew(cat, marker, Capacity);
                _recorders.Add(new Tracked
                {
                    Recorder = rec,
                    Label = label,
                    Category = cat,
                    Marker = marker,
                    Unit = ProfilerMarkerDataUnit.TimeNanoseconds
                });
            }
            catch { /* marker may not exist on this Unity version; ignore */ }
        }

        static void AddCount(string label, ProfilerCategory cat, string marker, ProfilerMarkerDataUnit unit)
        {
            try
            {
                var rec = ProfilerRecorder.StartNew(cat, marker, Capacity);
                _recorders.Add(new Tracked
                {
                    Recorder = rec,
                    Label = label,
                    Category = cat,
                    Marker = marker,
                    Unit = unit
                });
            }
            catch { }
        }

        public class MarkerStats
        {
            public string Label;
            public string Unit;            // "ms", "MB", "count"
            public int SampleCount;
            public double Avg;
            public double Min;
            public double Max;
            public double P95;
        }

        public class CaptureResult
        {
            public double DurationSeconds;
            public int FrameCount;           // estimated from "PlayerLoop" sample count
            public bool RolledOver;          // capacity exceeded
            public List<MarkerStats> Markers = new List<MarkerStats>();
        }

        public static CaptureResult Stop(bool silent = false)
        {
            if (!_active) return null;

            var result = new CaptureResult
            {
                DurationSeconds = (DateTime.UtcNow - _startedAt).TotalSeconds
            };

            foreach (var t in _recorders)
            {
                int count = t.Recorder.Count;
                if (count <= 0)
                {
                    t.Recorder.Dispose();
                    continue;
                }

                var samples = new double[count];
                for (int i = 0; i < count; i++)
                    samples[i] = t.Recorder.GetSample(i).Value;

                double scale = 1.0;
                string unit = "raw";
                switch (t.Unit)
                {
                    case ProfilerMarkerDataUnit.TimeNanoseconds:
                        scale = 1e-6; // ns → ms
                        unit = "ms";
                        break;
                    case ProfilerMarkerDataUnit.Bytes:
                        scale = 1.0 / (1024.0 * 1024.0); // bytes → MB
                        unit = "MB";
                        break;
                    case ProfilerMarkerDataUnit.Count:
                        unit = "count";
                        break;
                }

                double sum = 0, min = double.MaxValue, max = double.MinValue;
                for (int i = 0; i < count; i++)
                {
                    double v = samples[i] * scale;
                    samples[i] = v;
                    sum += v;
                    if (v < min) min = v;
                    if (v > max) max = v;
                }

                Array.Sort(samples);
                double p95 = samples[Mathf.Min(samples.Length - 1, Mathf.FloorToInt(samples.Length * 0.95f))];

                result.Markers.Add(new MarkerStats
                {
                    Label = t.Label,
                    Unit = unit,
                    SampleCount = count,
                    Avg = sum / count,
                    Min = min,
                    Max = max,
                    P95 = p95
                });

                if (t.Label == "PlayerLoop")
                {
                    result.FrameCount = count;
                    result.RolledOver = count >= Capacity;
                }

                t.Recorder.Dispose();
            }

            _recorders.Clear();
            _active = false;
            LastResult = result;
            LastResultAt = DateTime.UtcNow;
            if (!silent) OnStateChanged?.Invoke();
            return result;
        }

        public static string FormatResultAsText(CaptureResult r)
        {
            if (r == null) return "No capture data.";

            var sb = new StringBuilder();
            sb.AppendLine($"Profile capture: {r.DurationSeconds:F1}s, {r.FrameCount} frames" +
                          (r.RolledOver ? " (capacity exceeded — only last ~60s kept)" : ""));
            sb.AppendLine($"Avg FPS: {(r.FrameCount > 0 ? r.FrameCount / r.DurationSeconds : 0):F1}");
            sb.AppendLine();

            sb.AppendLine("CPU time (ms):");
            foreach (var m in r.Markers)
                if (m.Unit == "ms")
                    sb.AppendLine($"  {m.Label,-22} avg={m.Avg,7:F2}  min={m.Min,7:F2}  max={m.Max,7:F2}  p95={m.P95,7:F2}");

            sb.AppendLine();
            sb.AppendLine("Memory (MB):");
            foreach (var m in r.Markers)
                if (m.Unit == "MB")
                    sb.AppendLine($"  {m.Label,-22} avg={m.Avg,7:F2}  min={m.Min,7:F2}  max={m.Max,7:F2}  p95={m.P95,7:F2}");

            sb.AppendLine();
            sb.AppendLine("Render (per frame):");
            foreach (var m in r.Markers)
                if (m.Unit == "count")
                    sb.AppendLine($"  {m.Label,-22} avg={m.Avg,7:F0}  min={m.Min,7:F0}  max={m.Max,7:F0}  p95={m.P95,7:F0}");

            return sb.ToString();
        }
    }
}
