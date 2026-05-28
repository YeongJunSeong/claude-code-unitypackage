using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.MCP;

namespace ClaudeCode.Editor.UI
{
    public enum DiffLineType { Context, Added, Removed }

    public class DiffLine
    {
        public DiffLineType Type;
        public int OldLineNumber;
        public int NewLineNumber;
        public string Text;
    }

    public static class DiffView
    {
        public static bool IsDiffableTool(string toolName)
        {
            switch (toolName)
            {
                case "Write":
                case "Edit":
                case "MultiEdit":
                    return true;
                default:
                    return false;
            }
        }

        public static VisualElement Build(PermissionRequest request, Action<PermissionDecision> onDecision)
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.55f);
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;

            var box = new VisualElement();
            box.style.width = Length.Percent(85);
            box.style.maxWidth = 900;
            box.style.maxHeight = Length.Percent(85);
            box.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            box.style.borderTopLeftRadius = 10;
            box.style.borderTopRightRadius = 10;
            box.style.borderBottomLeftRadius = 10;
            box.style.borderBottomRightRadius = 10;
            box.style.paddingLeft = 18;
            box.style.paddingRight = 18;
            box.style.paddingTop = 16;
            box.style.paddingBottom = 14;
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderTopColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderBottomColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderLeftColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderRightColor = new Color(0.35f, 0.35f, 0.4f);

            BuildHeader(box, request);
            BuildBody(box, request);
            BuildFooter(box, overlay, onDecision);

            overlay.Add(box);
            return overlay;
        }

        static void BuildHeader(VisualElement box, PermissionRequest req)
        {
            var title = new Label("Permission Required");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 6;
            box.Add(title);

            var filePath = ExtractFilePath(req);
            var subtitle = new Label($"Claude wants to use {req.ToolName} on:");
            subtitle.style.fontSize = 11;
            subtitle.style.color = new Color(0.7f, 0.7f, 0.7f);
            subtitle.style.marginBottom = 4;
            box.Add(subtitle);

            var pathLabel = new Label(filePath ?? "(unknown path)");
            pathLabel.style.fontSize = 12;
            pathLabel.style.color = new Color(0.95f, 0.85f, 0.4f);
            pathLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            pathLabel.style.marginBottom = 10;
            pathLabel.selection.isSelectable = true;
            box.Add(pathLabel);
        }

        static void BuildBody(VisualElement box, PermissionRequest req)
        {
            var diff = BuildDiffLines(req);

            var summary = ComputeSummary(diff);
            var summaryLabel = new Label(summary);
            summaryLabel.style.fontSize = 10;
            summaryLabel.style.color = new Color(0.65f, 0.65f, 0.7f);
            summaryLabel.style.marginBottom = 6;
            box.Add(summaryLabel);

            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            scroll.style.maxHeight = 480;
            scroll.style.backgroundColor = new Color(0.11f, 0.11f, 0.13f);
            scroll.style.borderTopLeftRadius = 6;
            scroll.style.borderTopRightRadius = 6;
            scroll.style.borderBottomLeftRadius = 6;
            scroll.style.borderBottomRightRadius = 6;
            scroll.style.marginBottom = 12;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;

            if (diff == null || diff.Count == 0)
            {
                var empty = new Label("(no diff available — Claude may use raw text input)");
                empty.style.color = new Color(0.6f, 0.6f, 0.6f);
                empty.style.paddingLeft = 12;
                empty.style.paddingTop = 12;
                empty.style.paddingBottom = 12;
                scroll.Add(empty);
            }
            else
            {
                foreach (var line in diff)
                    scroll.Add(RenderLine(line));
            }

            box.Add(scroll);
        }

        static void BuildFooter(VisualElement box, VisualElement overlay, Action<PermissionDecision> onDecision)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.flexShrink = 0;

            Action<PermissionDecision> close = d =>
            {
                overlay.RemoveFromHierarchy();
                onDecision(d);
            };

            row.Add(MakeBtn("Deny", new Color(0.45f, 0.2f, 0.2f), 70, () => close(PermissionDecision.Deny)));

            var allowGroup = new VisualElement();
            allowGroup.style.flexDirection = FlexDirection.Row;

            allowGroup.Add(MakeBtn("Once", new Color(0.22f, 0.4f, 0.55f), 0, () => close(PermissionDecision.AllowOnce)));
            allowGroup.Add(Spacer(4));
            allowGroup.Add(MakeBtn("Session", new Color(0.22f, 0.5f, 0.4f), 0, () => close(PermissionDecision.AllowForSession)));
            allowGroup.Add(Spacer(4));
            allowGroup.Add(MakeBtn("Always", new Color(0.4f, 0.55f, 0.22f), 0, () => close(PermissionDecision.AllowAlways)));
            row.Add(allowGroup);

            box.Add(row);
        }

        static Button MakeBtn(string text, Color bg, float width, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.height = 30;
            b.style.paddingLeft = 12;
            b.style.paddingRight = 12;
            b.style.backgroundColor = bg;
            b.style.color = Color.white;
            b.style.borderTopLeftRadius = 6;
            b.style.borderTopRightRadius = 6;
            b.style.borderBottomLeftRadius = 6;
            b.style.borderBottomRightRadius = 6;
            b.style.fontSize = 11;
            if (width > 0) b.style.width = width;
            return b;
        }

        static VisualElement Spacer(int w)
        {
            var s = new VisualElement();
            s.style.width = w;
            return s;
        }

        // ---- Diff computation ----

        static string ExtractFilePath(PermissionRequest req)
        {
            if (req.RawInput == null) return null;
            if (req.RawInput.TryGetValue("file_path", out var v)) return v?.ToString();
            if (req.RawInput.TryGetValue("path", out v)) return v?.ToString();
            return null;
        }

        static List<DiffLine> BuildDiffLines(PermissionRequest req)
        {
            try
            {
                var filePath = ExtractFilePath(req);
                if (string.IsNullOrEmpty(filePath)) return null;

                var absPath = ToAbsolute(filePath);
                string original = File.Exists(absPath) ? File.ReadAllText(absPath) : "";
                string updated = ApplyEdit(req, original);
                if (updated == null) return null;
                return ComputeLineDiff(original, updated);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ClaudeCode] DiffView build failed: {e.Message}");
                return null;
            }
        }

        static string ToAbsolute(string relativeOrAbsolute)
        {
            if (Path.IsPathRooted(relativeOrAbsolute)) return relativeOrAbsolute;
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot ?? "", relativeOrAbsolute));
        }

        static string ApplyEdit(PermissionRequest req, string original)
        {
            switch (req.ToolName)
            {
                case "Write":
                    if (req.RawInput.TryGetValue("content", out var c)) return c?.ToString() ?? "";
                    return null;

                case "Edit":
                    if (req.RawInput.TryGetValue("old_string", out var os) &&
                        req.RawInput.TryGetValue("new_string", out var ns))
                    {
                        var oldStr = os?.ToString() ?? "";
                        var newStr = ns?.ToString() ?? "";
                        int idx = original.IndexOf(oldStr, StringComparison.Ordinal);
                        if (idx < 0) return original; // can't locate
                        return original.Substring(0, idx) + newStr + original.Substring(idx + oldStr.Length);
                    }
                    return null;

                case "MultiEdit":
                    if (req.RawInput.TryGetValue("edits", out var ev) && ev is List<object> editList)
                    {
                        string current = original;
                        foreach (var e in editList)
                        {
                            if (e is Dictionary<string, object> edit &&
                                edit.TryGetValue("old_string", out var eo) &&
                                edit.TryGetValue("new_string", out var en))
                            {
                                var oldStr = eo?.ToString() ?? "";
                                var newStr = en?.ToString() ?? "";
                                int idx = current.IndexOf(oldStr, StringComparison.Ordinal);
                                if (idx >= 0)
                                    current = current.Substring(0, idx) + newStr + current.Substring(idx + oldStr.Length);
                            }
                        }
                        return current;
                    }
                    return null;
            }
            return null;
        }

        static List<DiffLine> ComputeLineDiff(string oldText, string newText)
        {
            var oldLines = (oldText ?? "").Split('\n');
            var newLines = (newText ?? "").Split('\n');
            int n = oldLines.Length, m = newLines.Length;

            var lcs = new int[n + 1, m + 1];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    lcs[i + 1, j + 1] = (oldLines[i] == newLines[j])
                        ? lcs[i, j] + 1
                        : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);

            var result = new List<DiffLine>();
            int io = n, jn = m;
            while (io > 0 && jn > 0)
            {
                if (oldLines[io - 1] == newLines[jn - 1])
                {
                    result.Insert(0, new DiffLine { Type = DiffLineType.Context, OldLineNumber = io, NewLineNumber = jn, Text = oldLines[io - 1] });
                    io--; jn--;
                }
                else if (lcs[io, jn - 1] >= lcs[io - 1, jn])
                {
                    result.Insert(0, new DiffLine { Type = DiffLineType.Added, OldLineNumber = -1, NewLineNumber = jn, Text = newLines[jn - 1] });
                    jn--;
                }
                else
                {
                    result.Insert(0, new DiffLine { Type = DiffLineType.Removed, OldLineNumber = io, NewLineNumber = -1, Text = oldLines[io - 1] });
                    io--;
                }
            }
            while (io > 0) { result.Insert(0, new DiffLine { Type = DiffLineType.Removed, OldLineNumber = io, NewLineNumber = -1, Text = oldLines[io - 1] }); io--; }
            while (jn > 0) { result.Insert(0, new DiffLine { Type = DiffLineType.Added, OldLineNumber = -1, NewLineNumber = jn, Text = newLines[jn - 1] }); jn--; }

            // Trim context lines to only show near changes (e.g., 3 lines before/after each change)
            return TrimContext(result, contextLines: 3);
        }

        static List<DiffLine> TrimContext(List<DiffLine> diff, int contextLines)
        {
            if (diff == null || diff.Count == 0) return diff;

            // Mark lines to keep (changes + context around them)
            var keep = new bool[diff.Count];
            for (int i = 0; i < diff.Count; i++)
            {
                if (diff[i].Type != DiffLineType.Context)
                {
                    for (int k = Math.Max(0, i - contextLines); k <= Math.Min(diff.Count - 1, i + contextLines); k++)
                        keep[k] = true;
                }
            }

            var result = new List<DiffLine>();
            bool prevSkipped = false;
            for (int i = 0; i < diff.Count; i++)
            {
                if (keep[i])
                {
                    if (prevSkipped)
                    {
                        result.Add(new DiffLine { Type = DiffLineType.Context, OldLineNumber = -2, NewLineNumber = -2, Text = "..." });
                        prevSkipped = false;
                    }
                    result.Add(diff[i]);
                }
                else
                {
                    prevSkipped = true;
                }
            }
            return result;
        }

        static string ComputeSummary(List<DiffLine> diff)
        {
            if (diff == null || diff.Count == 0) return "No diff to display.";
            int added = 0, removed = 0;
            foreach (var d in diff)
            {
                if (d.Type == DiffLineType.Added) added++;
                else if (d.Type == DiffLineType.Removed) removed++;
            }
            return $"+{added} added, −{removed} removed";
        }

        static VisualElement RenderLine(DiffLine line)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Stretch;
            row.style.minHeight = 16;

            // Gutter (line numbers)
            var gutter = new Label();
            gutter.style.width = 70;
            gutter.style.fontSize = 10;
            gutter.style.color = new Color(0.5f, 0.5f, 0.55f);
            gutter.style.unityTextAlign = TextAnchor.UpperRight;
            gutter.style.paddingRight = 8;
            gutter.style.paddingLeft = 4;
            gutter.style.flexShrink = 0;

            string oldNum = line.OldLineNumber > 0 ? line.OldLineNumber.ToString() : "";
            string newNum = line.NewLineNumber > 0 ? line.NewLineNumber.ToString() : "";
            gutter.text = $"{oldNum,3} {newNum,3}";

            // Marker (+ / - / blank)
            var marker = new Label();
            marker.style.width = 16;
            marker.style.fontSize = 11;
            marker.style.unityTextAlign = TextAnchor.UpperCenter;
            marker.style.unityFontStyleAndWeight = FontStyle.Bold;
            marker.style.flexShrink = 0;

            // Content
            var content = new Label(line.Text);
            content.style.fontSize = 11;
            content.style.whiteSpace = WhiteSpace.Normal;
            content.style.flexGrow = 1;
            content.style.paddingLeft = 4;
            content.style.paddingRight = 8;
            content.selection.isSelectable = true;

            switch (line.Type)
            {
                case DiffLineType.Added:
                    row.style.backgroundColor = new Color(0.13f, 0.32f, 0.18f);
                    marker.text = "+";
                    marker.style.color = new Color(0.45f, 0.95f, 0.55f);
                    content.style.color = new Color(0.85f, 0.95f, 0.85f);
                    break;
                case DiffLineType.Removed:
                    row.style.backgroundColor = new Color(0.35f, 0.14f, 0.16f);
                    marker.text = "−";
                    marker.style.color = new Color(0.95f, 0.5f, 0.5f);
                    content.style.color = new Color(0.95f, 0.85f, 0.85f);
                    break;
                case DiffLineType.Context:
                default:
                    row.style.backgroundColor = new Color(0, 0, 0, 0);
                    if (line.OldLineNumber == -2)
                    {
                        gutter.text = "";
                        marker.text = "";
                        content.style.color = new Color(0.4f, 0.5f, 0.6f);
                        content.style.unityFontStyleAndWeight = FontStyle.Italic;
                    }
                    else
                    {
                        marker.text = " ";
                        content.style.color = new Color(0.78f, 0.78f, 0.8f);
                    }
                    break;
            }

            row.Add(gutter);
            row.Add(marker);
            row.Add(content);

            return row;
        }
    }
}
