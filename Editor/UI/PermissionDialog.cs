using System;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.MCP;

namespace ClaudeCode.Editor.UI
{
    public static class PermissionDialog
    {
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
            box.style.width = 460;
            box.style.maxHeight = Length.Percent(90);
            box.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            box.style.borderTopLeftRadius = 10;
            box.style.borderTopRightRadius = 10;
            box.style.borderBottomLeftRadius = 10;
            box.style.borderBottomRightRadius = 10;
            box.style.paddingLeft = 18;
            box.style.paddingRight = 18;
            box.style.paddingTop = 16;
            box.style.paddingBottom = 16;
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderTopColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderBottomColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderLeftColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderRightColor = new Color(0.35f, 0.35f, 0.4f);

            var title = new Label("Permission Required");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 10;
            box.Add(title);

            var subtitle = new Label("Claude wants to use a tool:");
            subtitle.style.fontSize = 11;
            subtitle.style.color = new Color(0.7f, 0.7f, 0.7f);
            subtitle.style.marginBottom = 6;
            box.Add(subtitle);

            var toolBox = new VisualElement();
            toolBox.style.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            toolBox.style.paddingLeft = 10;
            toolBox.style.paddingRight = 10;
            toolBox.style.paddingTop = 8;
            toolBox.style.paddingBottom = 8;
            toolBox.style.borderTopLeftRadius = 6;
            toolBox.style.borderTopRightRadius = 6;
            toolBox.style.borderBottomLeftRadius = 6;
            toolBox.style.borderBottomRightRadius = 6;
            toolBox.style.marginBottom = 14;
            toolBox.style.flexShrink = 1;
            toolBox.style.overflow = Overflow.Hidden;

            var toolName = new Label(request.ToolName);
            toolName.style.color = new Color(0.95f, 0.85f, 0.4f);
            toolName.style.fontSize = 13;
            toolName.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolName.style.marginBottom = 4;
            toolName.style.flexShrink = 0;
            toolBox.Add(toolName);

            var inputScroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            inputScroll.style.maxHeight = 220;
            inputScroll.style.flexShrink = 1;
            inputScroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            inputScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;

            var input = new Label(request.ToolInput);
            input.style.color = new Color(0.85f, 0.85f, 0.85f);
            input.style.fontSize = 11;
            input.style.whiteSpace = WhiteSpace.Normal;
            input.selection.isSelectable = true;
            inputScroll.Add(input);

            toolBox.Add(inputScroll);
            box.Add(toolBox);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.SpaceBetween;
            buttonRow.style.alignItems = Align.Center;
            buttonRow.style.flexShrink = 0;

            Action<PermissionDecision> close = decision =>
            {
                overlay.RemoveFromHierarchy();
                onDecision(decision);
            };

            var denyBtn = MakeButton("Deny", new Color(0.45f, 0.2f, 0.2f), () => close(PermissionDecision.Deny));
            denyBtn.style.width = 70;
            buttonRow.Add(denyBtn);

            var allowGroup = new VisualElement();
            allowGroup.style.flexDirection = FlexDirection.Row;

            var onceBtn = MakeButton("Once", new Color(0.22f, 0.4f, 0.55f), () => close(PermissionDecision.AllowOnce));
            onceBtn.style.marginRight = 4;
            allowGroup.Add(onceBtn);

            var sessionBtn = MakeButton("Session", new Color(0.22f, 0.5f, 0.4f), () => close(PermissionDecision.AllowForSession));
            sessionBtn.style.marginRight = 4;
            allowGroup.Add(sessionBtn);

            var alwaysBtn = MakeButton("Always", new Color(0.4f, 0.55f, 0.22f), () => close(PermissionDecision.AllowAlways));
            allowGroup.Add(alwaysBtn);

            buttonRow.Add(allowGroup);

            box.Add(buttonRow);
            overlay.Add(box);

            return overlay;
        }

        static Button MakeButton(string text, Color bg, Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.height = 30;
            btn.style.paddingLeft = 12;
            btn.style.paddingRight = 12;
            btn.style.backgroundColor = bg;
            btn.style.color = Color.white;
            btn.style.borderTopLeftRadius = 6;
            btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6;
            btn.style.borderBottomRightRadius = 6;
            btn.style.fontSize = 11;
            return btn;
        }
    }
}
