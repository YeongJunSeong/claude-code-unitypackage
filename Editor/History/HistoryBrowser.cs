using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClaudeCode.Editor.History
{
    public static class HistoryBrowser
    {
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

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            sidebar.Add(scroll);

            var sessions = HistoryStorage.ListSessions();
            if (sessions.Count == 0)
            {
                var empty = new Label("No recent sessions");
                empty.style.color = new Color(0.45f, 0.45f, 0.45f);
                empty.style.fontSize = 11;
                empty.style.paddingLeft = 12;
                empty.style.paddingTop = 12;
                scroll.Add(empty);
            }
            else
            {
                foreach (var s in sessions)
                    scroll.Add(BuildItem(s, s.sessionId == currentSessionId, onSelect, onDelete));
            }

            return sidebar;
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

            var dateLabel = new Label(FormatDate(record.lastMessageAt));
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
