using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.Core;

namespace ClaudeCode.Editor.History
{
    public static class HistoryBrowser
    {
        // Persists while the editor session lives so the filter survives sidebar rebuilds
        // (toggle, session load/delete all rebuild the sidebar).
        static string s_searchQuery = "";

        public static VisualElement Build(string currentSessionId, Action<SessionRecord> onSelect, Action<string> onDelete, Action onNewSession)
        {
            var sidebar = new VisualElement();
            sidebar.style.width = 240;
            sidebar.style.backgroundColor = new Color(0.12f, 0.12f, 0.13f);
            sidebar.style.borderRightWidth = 1;
            sidebar.style.borderRightColor = new Color(0.08f, 0.08f, 0.08f);
            sidebar.style.flexDirection = FlexDirection.Column;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.paddingLeft = 10;
            header.style.paddingRight = 6;
            header.style.height = 28;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.08f, 0.08f, 0.08f);

            var title = new Label("Recents");
            title.style.fontSize = 11;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.7f, 0.7f, 0.7f);
            header.Add(title);

            var newBtn = new Button(() => onNewSession?.Invoke()) { text = "+ New" };
            newBtn.style.fontSize = 10;
            newBtn.style.height = 20;
            newBtn.style.paddingLeft = 6;
            newBtn.style.paddingRight = 6;
            header.Add(newBtn);

            sidebar.Add(header);

            // ---- 검색 필드 ----
            var searchField = new TextField();
            searchField.value = s_searchQuery;
            searchField.style.marginLeft = 6;
            searchField.style.marginRight = 6;
            searchField.style.marginTop = 6;
            searchField.style.marginBottom = 2;
            var searchInput = searchField.Q<VisualElement>("unity-text-input");
            if (searchInput != null)
            {
                searchInput.style.paddingLeft = 6;
                searchInput.style.paddingRight = 6;
                searchInput.style.paddingTop = 3;
                searchInput.style.paddingBottom = 3;
                searchInput.style.backgroundColor = new Color(0.16f, 0.16f, 0.18f);
            }
            sidebar.Add(searchField);

            var searchHint = new Label("제목/대화 내용 검색");
            searchHint.style.fontSize = 8;
            searchHint.style.color = new Color(0.42f, 0.42f, 0.46f);
            searchHint.style.marginLeft = 8;
            searchHint.style.marginBottom = 4;
            sidebar.Add(searchHint);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            sidebar.Add(scroll);

            var sessions = HistoryStorage.ListSessions();

            void Rebuild(string query)
            {
                scroll.Clear();
                var filtered = Filter(sessions, query);

                if (filtered.Count == 0)
                {
                    var empty = new Label(string.IsNullOrEmpty(query) ? "No recent sessions" : "검색 결과 없음");
                    empty.style.color = new Color(0.45f, 0.45f, 0.45f);
                    empty.style.fontSize = 11;
                    empty.style.paddingLeft = 12;
                    empty.style.paddingTop = 12;
                    scroll.Add(empty);
                    return;
                }

                foreach (var s in filtered)
                    scroll.Add(BuildItem(s, s.sessionId == currentSessionId, onSelect, onDelete));
            }

            searchField.RegisterValueChangedCallback(evt =>
            {
                s_searchQuery = evt.newValue ?? "";
                Rebuild(s_searchQuery);
            });

            Rebuild(s_searchQuery);
            return sidebar;
        }

        static List<SessionRecord> Filter(List<SessionRecord> sessions, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return sessions;

            var q = query.Trim();
            var result = new List<SessionRecord>();
            foreach (var s in sessions)
            {
                if (Matches(s, q)) result.Add(s);
            }
            return result;
        }

        static bool Matches(SessionRecord s, string q)
        {
            if (ContainsIgnoreCase(s.GetDisplayTitle(), q)) return true;
            if (s.messages != null)
            {
                foreach (var m in s.messages)
                    if (ContainsIgnoreCase(m.content, q)) return true;
            }
            return false;
        }

        static bool ContainsIgnoreCase(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(haystack)) return false;
            return haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static VisualElement BuildItem(SessionRecord record, bool isActive, Action<SessionRecord> onSelect, Action<string> onDelete)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.paddingLeft = 10;
            item.style.paddingRight = 6;
            item.style.paddingTop = 6;
            item.style.paddingBottom = 6;
            item.style.borderBottomWidth = 1;
            item.style.borderBottomColor = new Color(0.16f, 0.16f, 0.17f);

            if (isActive)
                item.style.backgroundColor = new Color(0.22f, 0.28f, 0.4f);

            item.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (!isActive) item.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            });
            item.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (!isActive) item.style.backgroundColor = new Color(0, 0, 0, 0);
            });

            var dot = new VisualElement();
            dot.style.width = 6;
            dot.style.height = 6;
            dot.style.borderTopLeftRadius = 3;
            dot.style.borderTopRightRadius = 3;
            dot.style.borderBottomLeftRadius = 3;
            dot.style.borderBottomRightRadius = 3;
            dot.style.backgroundColor = isActive ? new Color(0.4f, 0.7f, 1f) : new Color(0.35f, 0.35f, 0.35f);
            dot.style.marginRight = 8;
            item.Add(dot);

            var textBox = new VisualElement();
            textBox.style.flexGrow = 1;
            textBox.style.flexShrink = 1;
            textBox.style.overflow = Overflow.Hidden;

            var label = new Label(record.GetDisplayTitle());
            label.style.color = isActive ? Color.white : new Color(0.85f, 0.85f, 0.85f);
            label.style.fontSize = 11;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            textBox.Add(label);

            var dateLabel = new Label(BuildMetaText(record));
            dateLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            dateLabel.style.fontSize = 9;
            dateLabel.style.marginTop = 2;
            textBox.Add(dateLabel);

            item.Add(textBox);

            var delBtn = new Button(() =>
            {
                if (UnityEditor.EditorUtility.DisplayDialog("Delete Session", "Delete this conversation?", "Delete", "Cancel"))
                    onDelete?.Invoke(record.sessionId);
            }) { text = "X" };
            delBtn.style.width = 18;
            delBtn.style.height = 18;
            delBtn.style.fontSize = 9;
            delBtn.style.paddingLeft = 0;
            delBtn.style.paddingRight = 0;
            delBtn.style.marginLeft = 4;
            delBtn.style.opacity = 0.5f;
            item.Add(delBtn);

            item.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.target == delBtn || (evt.target is VisualElement v && v.parent == delBtn)) return;
                onSelect?.Invoke(record);
            });

            return item;
        }

        // "2h ago · out 5.2k · $0.42" — usage parts only appear when the session has data.
        static string BuildMetaText(SessionRecord record)
        {
            var meta = FormatDate(record.lastMessageAt);

            int outTokens = 0;
            double cost = 0;
            if (record.messages != null)
            {
                foreach (var m in record.messages)
                {
                    if (m == null || !m.hasUsage) continue;
                    outTokens += m.outputTokens;
                    cost += m.costUsd;
                }
            }

            if (outTokens > 0)
                meta += $" · out {FormatTokens(outTokens)}";
            if (cost > 0)
                meta += $" · ${cost:0.00}";
            return meta;
        }

        static string FormatTokens(int n)
        {
            if (n >= 1_000_000) return (n / 1_000_000.0).ToString("0.#") + "M";
            if (n >= 1_000) return (n / 1_000.0).ToString("0.#") + "k";
            return n.ToString();
        }

        static string FormatDate(string isoDate)
        {
            if (string.IsNullOrEmpty(isoDate)) return "";
            if (!DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return "";

            var now = DateTime.Now;
            var span = now - dt;

            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return dt.ToString("yyyy-MM-dd");
        }
    }
}
