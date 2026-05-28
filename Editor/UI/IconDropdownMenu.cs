using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClaudeCode.Editor.UI
{
    /// <summary>
    /// Reusable overlay-based dropdown menu with VectorIcons.
    /// Used as a UI Toolkit replacement for PopupField/GenericMenu when we
    /// need custom icons on items.
    /// </summary>
    public static class IconDropdownMenu
    {
        public class Item
        {
            public IconType Icon;
            public string Label;
            public string Description;
            public bool Disabled;
            public bool IsCurrent;
            public Action OnClick;
        }

        public static VisualElement Build(VisualElement source, List<Item> items, int menuWidth = 240, bool anchorRight = false)
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.target == overlay) overlay.RemoveFromHierarchy();
            });

            var menu = new VisualElement();
            menu.style.position = Position.Absolute;
            menu.style.width = menuWidth;
            menu.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            menu.style.borderTopLeftRadius = 8;
            menu.style.borderTopRightRadius = 8;
            menu.style.borderBottomLeftRadius = 8;
            menu.style.borderBottomRightRadius = 8;
            menu.style.paddingTop = 6;
            menu.style.paddingBottom = 6;
            menu.style.borderTopWidth = 1;
            menu.style.borderBottomWidth = 1;
            menu.style.borderLeftWidth = 1;
            menu.style.borderRightWidth = 1;
            menu.style.borderTopColor = new Color(0.32f, 0.32f, 0.36f);
            menu.style.borderBottomColor = new Color(0.32f, 0.32f, 0.36f);
            menu.style.borderLeftColor = new Color(0.32f, 0.32f, 0.36f);
            menu.style.borderRightColor = new Color(0.32f, 0.32f, 0.36f);

            // Position relative to the source button.
            // worldBound is window-relative which matches the overlay coordinate space.
            var src = source.worldBound;
            menu.style.top = src.y + src.height + 2;
            if (anchorRight)
                menu.style.left = src.x + src.width - menuWidth;
            else
                menu.style.left = src.x;

            foreach (var it in items)
                menu.Add(BuildRow(it, () => overlay.RemoveFromHierarchy()));

            overlay.Add(menu);
            return overlay;
        }

        static VisualElement BuildRow(Item item, Action close)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;

            if (item.IsCurrent)
                row.style.backgroundColor = new Color(0.22f, 0.32f, 0.45f);

            if (!item.Disabled)
            {
                row.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (!item.IsCurrent)
                        row.style.backgroundColor = new Color(0.25f, 0.25f, 0.3f);
                });
                row.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    if (!item.IsCurrent)
                        row.style.backgroundColor = new Color(0, 0, 0, 0);
                });
                row.RegisterCallback<MouseDownEvent>(_ =>
                {
                    close();
                    item.OnClick?.Invoke();
                });
            }

            var iconColor = item.Disabled
                ? new Color(0.5f, 0.5f, 0.55f)
                : new Color(0.86f, 0.86f, 0.88f);
            var icon = VectorIcons.Make(item.Icon, 14, iconColor);
            icon.style.marginRight = 10;
            row.Add(icon);

            var textBox = new VisualElement();
            textBox.style.flexGrow = 1;
            textBox.style.flexShrink = 1;
            textBox.style.overflow = Overflow.Hidden;

            var label = new Label(item.Label);
            label.style.fontSize = 12;
            label.style.color = item.Disabled
                ? new Color(0.5f, 0.5f, 0.55f)
                : new Color(0.92f, 0.92f, 0.92f);
            label.style.unityFontStyleAndWeight = item.IsCurrent ? FontStyle.Bold : FontStyle.Normal;
            textBox.Add(label);

            if (!string.IsNullOrEmpty(item.Description))
            {
                var desc = new Label(item.Description);
                desc.style.fontSize = 9;
                desc.style.color = new Color(0.55f, 0.55f, 0.6f);
                desc.style.marginTop = 1;
                desc.style.whiteSpace = WhiteSpace.Normal;
                textBox.Add(desc);
            }

            row.Add(textBox);

            if (item.IsCurrent)
            {
                var check = VectorIcons.Make(IconType.Check, 12, new Color(0.5f, 0.85f, 1f));
                check.style.marginLeft = 8;
                row.Add(check);
            }

            return row;
        }
    }
}
