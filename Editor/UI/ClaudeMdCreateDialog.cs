using System;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.Core;

namespace ClaudeCode.Editor.UI
{
    public static class ClaudeMdCreateDialog
    {
        public static VisualElement Build(Action<string> onConfirm)
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
            box.style.width = 520;
            box.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            box.style.borderTopLeftRadius = 10;
            box.style.borderTopRightRadius = 10;
            box.style.borderBottomLeftRadius = 10;
            box.style.borderBottomRightRadius = 10;
            box.style.paddingLeft = 22;
            box.style.paddingRight = 22;
            box.style.paddingTop = 20;
            box.style.paddingBottom = 18;
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderTopColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderBottomColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderLeftColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderRightColor = new Color(0.35f, 0.35f, 0.4f);

            var title = new Label("CLAUDE.md 생성");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 6;
            box.Add(title);

            var subtitle = new Label("다음 경로에 CLAUDE.md 파일을 생성하시겠습니까?\n경로를 직접 수정할 수도 있습니다.");
            subtitle.style.fontSize = 11;
            subtitle.style.color = new Color(0.75f, 0.75f, 0.78f);
            subtitle.style.marginBottom = 12;
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            box.Add(subtitle);

            var pathLabel = new Label("경로");
            pathLabel.style.fontSize = 10;
            pathLabel.style.color = new Color(0.6f, 0.6f, 0.65f);
            pathLabel.style.marginBottom = 4;
            box.Add(pathLabel);

            var defaultPath = ClaudeMdManager.GetProjectClaudeMdPath().Replace("\\", "/");
            var pathField = new TextField();
            pathField.value = defaultPath;
            pathField.style.minHeight = 28;
            pathField.style.marginBottom = 8;
            pathField.style.overflow = Overflow.Hidden;
            var inputEl = pathField.Q<VisualElement>("unity-text-input");
            if (inputEl != null)
            {
                inputEl.style.paddingLeft = 8;
                inputEl.style.paddingRight = 8;
                inputEl.style.minWidth = 0;
                inputEl.style.flexShrink = 1;
            }
            box.Add(pathField);

            var feedback = new Label("");
            feedback.style.fontSize = 10;
            feedback.style.whiteSpace = WhiteSpace.Normal;
            feedback.style.marginBottom = 14;
            box.Add(feedback);

            var hint = new Label($"프로젝트 루트: {ClaudeMdManager.GetProjectRoot().Replace("\\", "/")}");
            hint.style.fontSize = 9;
            hint.style.color = new Color(0.5f, 0.5f, 0.55f);
            hint.style.marginBottom = 14;
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.selection.isSelectable = true;
            box.Add(hint);

            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;

            var cancelBtn = new Button(() => overlay.RemoveFromHierarchy()) { text = "Cancel" };
            cancelBtn.style.width = 80;
            cancelBtn.style.height = 28;
            cancelBtn.style.marginRight = 8;
            footer.Add(cancelBtn);

            var createBtn = new Button { text = "Create" };
            createBtn.style.width = 90;
            createBtn.style.height = 28;
            createBtn.style.backgroundColor = new Color(0.2f, 0.55f, 0.3f);
            createBtn.style.color = Color.white;
            createBtn.style.borderTopLeftRadius = 6;
            createBtn.style.borderTopRightRadius = 6;
            createBtn.style.borderBottomLeftRadius = 6;
            createBtn.style.borderBottomRightRadius = 6;
            footer.Add(createBtn);

            box.Add(footer);

            Action validateAndUpdate = () =>
            {
                var result = ClaudeMdManager.ValidatePath(pathField.value);
                if (result.IsValid)
                {
                    var msg = string.IsNullOrEmpty(result.ErrorMessage)
                        ? $"OK 생성 가능: {result.NormalizedPath.Replace("\\", "/")}"
                        : result.ErrorMessage;
                    feedback.text = msg;
                    feedback.style.color = string.IsNullOrEmpty(result.ErrorMessage)
                        ? new Color(0.4f, 0.85f, 0.5f)
                        : new Color(0.95f, 0.75f, 0.3f);
                    createBtn.SetEnabled(true);
                }
                else
                {
                    feedback.text = $"! {result.ErrorMessage}";
                    feedback.style.color = new Color(0.95f, 0.5f, 0.5f);
                    createBtn.SetEnabled(false);
                }
            };

            pathField.RegisterValueChangedCallback(_ => validateAndUpdate());
            validateAndUpdate();

            createBtn.clicked += () =>
            {
                var result = ClaudeMdManager.ValidatePath(pathField.value);
                if (!result.IsValid) return;
                overlay.RemoveFromHierarchy();
                onConfirm?.Invoke(result.NormalizedPath);
            };

            overlay.Add(box);
            return overlay;
        }
    }
}
