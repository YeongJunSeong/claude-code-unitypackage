using System.Collections.Generic;
using System.Text;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace ClaudeCode.Editor.Context
{
    /// <summary>
    /// Reads frame data already captured by Unity's Profiler window via ProfilerDriver +
    /// HierarchyFrameDataView. Aggregates per-marker stats across the requested frame range
    /// and produces a human-readable summary suitable for sending to Claude.
    /// </summary>
    public static class ProfilerSnapshot
    {
        struct MarkerAccum
        {
            public double TotalMsSum;     // sum across frames
            public double TotalMsMax;
            public int FrameHits;          // number of frames the marker appeared in
        }

        public class Result
        {
            public int FirstFrame;
            public int LastFrame;
            public int FrameCount;
            public double AvgFrameMs;
            public double MaxFrameMs;
            public int MaxFrameIndex;
            public double GcAllocBytesTotal;
            public List<(string Name, double AvgMs, double MaxMs, double SharePct)> TopMarkers
                = new List<(string, double, double, double)>();
            public string Note; // human-readable notes / warnings
        }

        public static Result CaptureAll()
        {
            int first = ProfilerDriver.firstFrameIndex;
            int last = ProfilerDriver.lastFrameIndex;
            if (last < first || last < 0)
                return new Result { Note = "No frames recorded in Unity Profiler." };

            return CaptureRange(first, last);
        }

        public static Result CaptureRange(int firstFrame, int lastFrame)
        {
            var result = new Result
            {
                FirstFrame = firstFrame,
                LastFrame = lastFrame
            };

            // marker name → accumulated stats
            var markers = new Dictionary<string, MarkerAccum>(64);
            double totalFrameMs = 0;
            int validFrames = 0;

            for (int f = firstFrame; f <= lastFrame; f++)
            {
                HierarchyFrameDataView data = null;
                try
                {
                    data = ProfilerDriver.GetHierarchyFrameDataView(
                        f,
                        threadIndex: 0,
                        viewMode: HierarchyFrameDataView.ViewModes.Default,
                        sortColumn: HierarchyFrameDataView.columnTotalTime,
                        sortAscending: false);

                    if (data == null || !data.valid) continue;

                    // FrameDataView.frameTimeNs exists in 2022.1+. Convert to ms.
                    double frameMs = data.frameTimeNs / 1_000_000.0;
                    totalFrameMs += frameMs;
                    if (frameMs > result.MaxFrameMs)
                    {
                        result.MaxFrameMs = frameMs;
                        result.MaxFrameIndex = f;
                    }
                    validFrames++;

                    // Aggregate top-level children (EditorLoop / PlayerLoop / Profiler.* / ...).
                    // Also sum their GC alloc to get a per-frame total.
                    int root = data.GetRootItemID();
                    var children = new List<int>();
                    data.GetItemChildren(root, children);
                    foreach (int childId in children)
                    {
                        string name = data.GetItemName(childId);
                        if (string.IsNullOrEmpty(name)) continue;
                        float ms = data.GetItemColumnDataAsSingle(childId, HierarchyFrameDataView.columnTotalTime);
                        float gcBytes = 0f;
                        try { gcBytes = data.GetItemColumnDataAsSingle(childId, HierarchyFrameDataView.columnGcMemory); } catch { }

                        if (!markers.TryGetValue(name, out var acc))
                            acc = new MarkerAccum();
                        acc.TotalMsSum += ms;
                        if (ms > acc.TotalMsMax) acc.TotalMsMax = ms;
                        acc.FrameHits++;
                        markers[name] = acc;

                        result.GcAllocBytesTotal += gcBytes;
                    }
                }
                catch { /* skip bad frame */ }
                finally
                {
                    data?.Dispose();
                }
            }

            result.FrameCount = validFrames;
            result.AvgFrameMs = validFrames > 0 ? totalFrameMs / validFrames : 0;

            // Build top markers, sorted by avg ms descending
            var sorted = new List<(string, double, double, double)>();
            foreach (var kv in markers)
            {
                double avg = kv.Value.TotalMsSum / System.Math.Max(1, kv.Value.FrameHits);
                double share = result.AvgFrameMs > 0 ? (avg / result.AvgFrameMs) * 100.0 : 0;
                sorted.Add((kv.Key, avg, kv.Value.TotalMsMax, share));
            }
            sorted.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            int take = System.Math.Min(10, sorted.Count);
            for (int i = 0; i < take; i++) result.TopMarkers.Add(sorted[i]);

            if (validFrames == 0)
                result.Note = "No valid frames in the requested range.";

            return result;
        }

        public static string FormatAsText(Result r)
        {
            if (r == null) return "(no data)";
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(r.Note))
            {
                sb.AppendLine(r.Note);
                return sb.ToString();
            }

            sb.AppendLine($"Profiler snapshot: frames {r.FirstFrame}–{r.LastFrame} ({r.FrameCount} valid frames)");
            sb.AppendLine($"Avg frame: {r.AvgFrameMs:F2} ms  ({(r.AvgFrameMs > 0 ? 1000.0 / r.AvgFrameMs : 0):F1} fps)");
            sb.AppendLine($"Max frame: {r.MaxFrameMs:F2} ms  at frame #{r.MaxFrameIndex}");
            sb.AppendLine($"Total GC allocated across range: {r.GcAllocBytesTotal / 1024.0:F1} KB");
            sb.AppendLine();
            sb.AppendLine("Top markers (avg ms/frame, share of avg frame):");
            foreach (var m in r.TopMarkers)
                sb.AppendLine($"  {m.Name,-32}  avg={m.AvgMs,7:F2} ms  max={m.MaxMs,7:F2} ms  ({m.SharePct,5:F1}%)");

            sb.AppendLine();
            sb.AppendLine("Note: \"EditorLoop\" is editor-only overhead and does NOT exist in real builds. PlayerLoop is the actual game frame work. Treat them separately.");
            return sb.ToString();
        }
    }
}
