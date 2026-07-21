using System;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.Core;

namespace ClaudeCode.Editor.UI
{
    /// <summary>
    /// Slider-style popup for picking the reasoning-effort level,
    /// modeled after the Claude desktop app's effort picker
    /// (더 빠름 ←→ 더 스마트함).
    /// </summary>
    public static class EffortPopup
    {
        public static VisualElement Build(VisualElement root, VisualElement anchor, Action onChanged)
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

            var box = new VisualElement();
            box.style.position = Position.Absolute;
            box.style.width = 240;
            box.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            box.style.borderTopLeftRadius = 10;
            box.style.borderTopRightRadius = 10;
            box.style.borderBottomLeftRadius = 10;
            box.style.borderBottomRightRadius = 10;
            box.style.paddingLeft = 14;
            box.style.paddingRight = 14;
            box.style.paddingTop = 10;
            box.style.paddingBottom = 12;
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            var borderColor = new Color(0.32f, 0.32f, 0.36f);
            box.style.borderTopColor = borderColor;
            box.style.borderBottomColor = borderColor;
            box.style.borderLeftColor = borderColor;
            box.style.borderRightColor = borderColor;

            // Anchor above the button (input row lives at the bottom of the window).
            var src = anchor.worldBound;
            float rootH = root.layout.height;
            box.style.bottom = rootH - src.y + 6;
            float left = src.x + src.width - 240;
            if (left < 8) left = 8;
            box.style.left = left;

            // ---- Title: "작업량 <레벨>" ----
            var title = new Label();
            title.style.fontSize = 12;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.92f, 0.92f, 0.92f);
            title.style.marginBottom = 8;
            box.Add(title);

            // ---- 더 빠름 / 더 스마트함 라벨 행 ----
            var labelRow = new VisualElement();
            labelRow.style.flexDirection = FlexDirection.Row;
            labelRow.style.justifyContent = Justify.SpaceBetween;
            labelRow.style.marginBottom = 2;

            var fasterLabel = new Label("더 빠름");
            fasterLabel.style.fontSize = 10;
            fasterLabel.style.color = new Color(0.6f, 0.6f, 0.65f);
            labelRow.Add(fasterLabel);

            var smarterLabel = new Label("더 스마트함");
            smarterLabel.style.fontSize = 10;
            smarterLabel.style.color = new Color(0.6f, 0.6f, 0.65f);
            labelRow.Add(smarterLabel);

            box.Add(labelRow);

            // ---- 슬라이더 (5단계) ----
            var slider = new SliderInt(0, EffortManager.Levels.Length - 1);
            slider.value = EffortManager.CurrentIndex;
            slider.style.marginBottom = 8;
            box.Add(slider);

            // ---- 기본값 사용 ----
            var defaultBtn = new Button { text = "기본값 사용 (CLI 설정)" };
            defaultBtn.style.fontSize = 10;
            defaultBtn.style.height = 20;
            defaultBtn.style.backgroundColor = new Color(0.24f, 0.24f, 0.28f);
            defaultBtn.style.color = new Color(0.8f, 0.8f, 0.85f);
            defaultBtn.style.borderTopLeftRadius = 4;
            defaultBtn.style.borderTopRightRadius = 4;
            defaultBtn.style.borderBottomLeftRadius = 4;
            defaultBtn.style.borderBottomRightRadius = 4;
            box.Add(defaultBtn);

            void RefreshTitle()
            {
                title.text = $"작업량  {EffortManager.CurrentDisplayName}";
                defaultBtn.style.display = EffortManager.IsDefault ? DisplayStyle.None : DisplayStyle.Flex;
            }
            RefreshTitle();

            slider.RegisterValueChangedCallback(evt =>
            {
                int idx = Mathf.Clamp(evt.newValue, 0, EffortManager.Levels.Length - 1);
                EffortManager.Current = EffortManager.Levels[idx];
                RefreshTitle();
                onChanged?.Invoke();
            });

            defaultBtn.clicked += () =>
            {
                EffortManager.Current = EffortManager.Default;
                slider.SetValueWithoutNotify(EffortManager.CurrentIndex);
                RefreshTitle();
                onChanged?.Invoke();
            };

            overlay.Add(box);
            return overlay;
        }
    }
}
