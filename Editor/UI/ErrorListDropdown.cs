using System;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.Context;

namespace ClaudeCode.Editor.UI
{
    public static class ErrorListDropdown
    {
        public static VisualElement Build(Action<ConsoleError> onFix, Action onClearAll)
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.alignItems = Align.FlexEnd;
            overlay.style.justifyContent = Justify.FlexStart;
            overlay.style.paddingTop = 30;
            overlay.style.paddingRight = 8;

            overlay.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.target == overlay) overlay.RemoveFromHierarchy();
            });

            var box = new VisualElement();
            box.style.width = 360;
            box.style.maxHeight = 400;
            box.style.backgroundColor = new Color(0.16f, 0.16f, 0.18f);
            box.style.borderTopLeftRadius = 8;
            box.style.borderTopRightRadius = 8;
            box.style.borderBottomLeftRadius = 8;
            box.style.borderBottomRightRadius = 8;
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderTopColor = new Color(0.34f, 0.34f, 0.38f);
            box.style.borderBottomColor = new Color(0.34f, 0.34f, 0.38f);
            box.style.borderLeftColor = new Color(0.34f, 0.34f, 0.38f);
            box.style.borderRightColor = new Color(0.34f, 0.34f, 0.38f);

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.paddingLeft = 12;
            header.style.paddingRight = 8;
            header.style.paddingTop = 8;
            header.style.paddingBottom = 8;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.24f, 0.24f, 0.28f);

            var title = new Label($"최근 에러 ({ConsoleLogProvider.ErrorCount}개)");
            title.style.fontSize = 11;
            title.style.color = new Color(0.92f, 0.92f, 0.92f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            var clearBtn = new Button(() =>
            {
                onClearAll?.Invoke();
                overlay.RemoveFromHierarchy();
            }) { text = "Clear all" };
            clearBtn.style.height = 20;
            clearBtn.style.fontSize = 10;
            clearBtn.style.paddingLeft = 8;
            clearBtn.style.paddingRight = 8;
            header.Add(clearBtn);

            box.Add(header);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;

            var errors = ConsoleLogProvider.Errors;
            if (errors.Count == 0)
            {
                var empty = new Label("최근 에러가 없습니다.");
                empty.style.color = new Color(0.55f, 0.55f, 0.6f);
                empty.style.fontSize = 11;
                empty.style.paddingLeft = 12;
                empty.style.paddingTop = 12;
                empty.style.paddingBottom = 12;
                scroll.Add(empty);
            }
            else
            {
                for (int i = errors.Count - 1; i >= 0; i--)
                {
                    var err = errors[i];
                    scroll.Add(BuildErrorItem(err, () =>
                    {
                        onFix?.Invoke(err);
                        overlay.RemoveFromHierarchy();
                    }));
                }
            }

            box.Add(scroll);
            overlay.Add(box);
            return overlay;
        }

        static VisualElement BuildErrorItem(ConsoleError err, Action onFix)
        {
            var item = new VisualElement();
            item.style.paddingLeft = 12;
            item.style.paddingRight = 12;
            item.style.paddingTop = 8;
            item.style.paddingBottom = 8;
            item.style.borderBottomWidth = 1;
            item.style.borderBottomColor = new Color(0.22f, 0.22f, 0.25f);

            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.alignItems = Align.FlexStart;

            var iconImg = VectorIcons.Make(IconType.Error, 14, new Color(0.95f, 0.5f, 0.5f));
            iconImg.style.marginRight = 6;
            iconImg.style.marginTop = 2;
            topRow.Add(iconImg);

            var textBox = new VisualElement();
            textBox.style.flexGrow = 1;
            textBox.style.flexShrink = 1;

            var msg = new Label(Truncate(err.Message, 120));
            msg.style.fontSize = 11;
            msg.style.color = new Color(0.95f, 0.85f, 0.85f);
            msg.style.unityFontStyleAndWeight = FontStyle.Bold;
            msg.style.whiteSpace = WhiteSpace.Normal;
            textBox.Add(msg);

            var meta = new Label(BuildMetaText(err));
            meta.style.fontSize = 9;
            meta.style.color = new Color(0.55f, 0.55f, 0.6f);
            meta.style.marginTop = 2;
            textBox.Add(meta);

            topRow.Add(textBox);
            item.Add(topRow);

            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.justifyContent = Justify.FlexEnd;
            actionRow.style.marginTop = 6;

            var fixBtn = new Button(onFix) { text = "Fix with Claude" };
            fixBtn.style.height = 22;
            fixBtn.style.fontSize = 10;
            fixBtn.style.paddingLeft = 10;
            fixBtn.style.paddingRight = 10;
            fixBtn.style.backgroundColor = new Color(0.25f, 0.45f, 0.75f);
            fixBtn.style.color = Color.white;
            fixBtn.style.borderTopLeftRadius = 4;
            fixBtn.style.borderTopRightRadius = 4;
            fixBtn.style.borderBottomLeftRadius = 4;
            fixBtn.style.borderBottomRightRadius = 4;
            actionRow.Add(fixBtn);

            item.Add(actionRow);
            return item;
        }

        static string BuildMetaText(ConsoleError err)
        {
            var loc = err.ShortLocation;
            var time = RelativeTime(err.Timestamp);
            if (string.IsNullOrEmpty(loc)) return time;
            return $"{loc}  •  {time}";
        }

        static string RelativeTime(DateTime t)
        {
            var span = DateTime.Now - t;
            if (span.TotalSeconds < 60) return "방금 전";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}분 전";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}시간 전";
            return t.ToString("MM-dd HH:mm");
        }

        static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max - 1) + "…";
        }
    }
}
