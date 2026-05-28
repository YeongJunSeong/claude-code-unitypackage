using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClaudeCode.Editor.UI
{
    public static class EditMessageDialog
    {
        public static VisualElement Build(string originalContent, Action<string> onResend)
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
            box.style.width = Length.Percent(70);
            box.style.maxWidth = 640;
            box.style.maxHeight = Length.Percent(80);
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

            var title = new Label("Edit and Resend");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 4;
            box.Add(title);

            var subtitle = new Label("메시지를 수정해서 다시 보냅니다. 이 메시지 이후의 응답들은 삭제됩니다.");
            subtitle.style.fontSize = 11;
            subtitle.style.color = new Color(0.7f, 0.7f, 0.7f);
            subtitle.style.marginBottom = 10;
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            box.Add(subtitle);

            var field = new TextField();
            field.multiline = true;
            field.value = originalContent ?? "";
            field.style.flexGrow = 1;
            field.style.minHeight = 100;
            field.style.maxHeight = 360;
            field.style.marginBottom = 12;

            var textInput = field.Q<VisualElement>("unity-text-input");
            if (textInput != null)
            {
                textInput.style.paddingLeft = 10;
                textInput.style.paddingRight = 10;
                textInput.style.paddingTop = 8;
                textInput.style.paddingBottom = 8;
                textInput.style.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
                textInput.style.minWidth = 0;
                textInput.style.whiteSpace = WhiteSpace.Normal;
            }
            box.Add(field);

            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;
            footer.style.flexShrink = 0;

            var cancelBtn = new Button(() => overlay.RemoveFromHierarchy()) { text = "Cancel" };
            cancelBtn.style.width = 80;
            cancelBtn.style.height = 28;
            cancelBtn.style.marginRight = 8;
            footer.Add(cancelBtn);

            var resendBtn = new Button(() =>
            {
                var newContent = field.value?.Trim();
                if (string.IsNullOrEmpty(newContent)) return;
                overlay.RemoveFromHierarchy();
                onResend?.Invoke(newContent);
            })
            { text = "Resend" };
            resendBtn.style.width = 90;
            resendBtn.style.height = 28;
            resendBtn.style.backgroundColor = new Color(0.2f, 0.55f, 0.3f);
            resendBtn.style.color = Color.white;
            resendBtn.style.borderTopLeftRadius = 6;
            resendBtn.style.borderTopRightRadius = 6;
            resendBtn.style.borderBottomLeftRadius = 6;
            resendBtn.style.borderBottomRightRadius = 6;
            footer.Add(resendBtn);

            box.Add(footer);
            overlay.Add(box);

            // Focus textarea
            overlay.schedule.Execute(() => field.Focus()).StartingIn(50);

            return overlay;
        }
    }
}
