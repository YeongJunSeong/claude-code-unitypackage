using UnityEngine;
using UnityEngine.UIElements;

namespace ClaudeCode.Editor.UI
{
    /// <summary>
    /// Small circular usage indicator with percentage text.
    /// 0~70% green, 70~90% yellow, 90~100% red.
    /// </summary>
    public class UsageIndicator : Label
    {
        public UsageIndicator()
        {
            style.width = 28;
            style.height = 28;
            style.fontSize = 9;
            style.unityFontStyleAndWeight = FontStyle.Bold;
            style.unityTextAlign = TextAnchor.MiddleCenter;
            style.borderTopLeftRadius = 14;
            style.borderTopRightRadius = 14;
            style.borderBottomLeftRadius = 14;
            style.borderBottomRightRadius = 14;
            style.paddingLeft = 0;
            style.paddingRight = 0;
            style.paddingTop = 0;
            style.paddingBottom = 0;
            style.color = Color.white;
            style.backgroundColor = new Color(0.28f, 0.28f, 0.32f);
            style.flexShrink = 0;
            text = "—";
        }

        public void SetValue(float v01, string tooltipText)
        {
            v01 = Mathf.Clamp01(v01);
            text = $"{Mathf.RoundToInt(v01 * 100)}";

            if (v01 >= 0.9f) style.backgroundColor = new Color(0.7f, 0.25f, 0.25f);
            else if (v01 >= 0.7f) style.backgroundColor = new Color(0.7f, 0.55f, 0.15f);
            else style.backgroundColor = new Color(0.2f, 0.5f, 0.3f);

            tooltip = tooltipText;
        }

        public void SetIdle(string tooltipText)
        {
            text = "—";
            style.backgroundColor = new Color(0.28f, 0.28f, 0.32f);
            tooltip = tooltipText;
        }
    }
}
