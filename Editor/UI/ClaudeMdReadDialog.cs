using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.Core;

namespace ClaudeCode.Editor.UI
{
    public static class ClaudeMdReadDialog
    {
        public static VisualElement Build()
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

            overlay.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.target == overlay) overlay.RemoveFromHierarchy();
            });

            var box = new VisualElement();
            box.style.width = Length.Percent(85);
            box.style.maxWidth = 720;
            box.style.maxHeight = Length.Percent(85);
            box.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            box.style.borderTopLeftRadius = 10;
            box.style.borderTopRightRadius = 10;
            box.style.borderBottomLeftRadius = 10;
            box.style.borderBottomRightRadius = 10;
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderTopColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderBottomColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderLeftColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderRightColor = new Color(0.35f, 0.35f, 0.4f);

            BuildHeader(box, overlay);
            BuildBody(box);
            BuildFooter(box, overlay);

            overlay.Add(box);
            return overlay;
        }

        static void BuildHeader(VisualElement box, VisualElement overlay)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.paddingLeft = 18;
            header.style.paddingRight = 12;
            header.style.paddingTop = 12;
            header.style.paddingBottom = 10;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.28f, 0.28f, 0.32f);
            header.style.flexShrink = 0;

            var titleGroup = new VisualElement();
            titleGroup.style.flexDirection = FlexDirection.Column;

            var title = new Label("CLAUDE.md");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            titleGroup.Add(title);

            var pathLabel = new Label(ClaudeMdManager.GetProjectClaudeMdPath());
            pathLabel.style.fontSize = 9;
            pathLabel.style.color = new Color(0.55f, 0.55f, 0.6f);
            pathLabel.style.marginTop = 2;
            pathLabel.selection.isSelectable = true;
            titleGroup.Add(pathLabel);

            header.Add(titleGroup);

            var closeBtn = new Button(() => overlay.RemoveFromHierarchy()) { text = "x" };
            closeBtn.style.width = 26;
            closeBtn.style.height = 26;
            closeBtn.style.fontSize = 11;
            closeBtn.style.paddingLeft = 0;
            closeBtn.style.paddingRight = 0;
            header.Add(closeBtn);

            box.Add(header);
        }

        static void BuildBody(VisualElement box)
        {
            var body = new ScrollView(ScrollViewMode.Vertical);
            body.style.flexGrow = 1;
            body.style.paddingLeft = 18;
            body.style.paddingRight = 18;
            body.style.paddingTop = 14;
            body.style.paddingBottom = 14;

            var content = ClaudeMdManager.Read();
            if (string.IsNullOrEmpty(content))
            {
                var notFound = new Label("CLAUDE.md를 찾을 수 없습니다.\n\n프로젝트 루트에 CLAUDE.md가 없습니다. 우상단의 CLAUDE.md ▶ Update를 클릭하면 Claude가 프로젝트 상태를 분석해서 새로 만들어줍니다.");
                notFound.style.color = new Color(0.7f, 0.7f, 0.75f);
                notFound.style.fontSize = 12;
                notFound.style.whiteSpace = WhiteSpace.Normal;
                body.Add(notFound);
            }
            else
            {
                var mdContainer = new VisualElement();
                MarkdownRenderer.Render(content, mdContainer);
                body.Add(mdContainer);
            }

            box.Add(body);
        }

        static void BuildFooter(VisualElement box, VisualElement overlay)
        {
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;
            footer.style.paddingLeft = 18;
            footer.style.paddingRight = 18;
            footer.style.paddingTop = 10;
            footer.style.paddingBottom = 14;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = new Color(0.28f, 0.28f, 0.32f);
            footer.style.flexShrink = 0;

            var closeBtn = new Button(() => overlay.RemoveFromHierarchy()) { text = "Close" };
            closeBtn.style.width = 80;
            closeBtn.style.height = 28;
            footer.Add(closeBtn);

            box.Add(footer);
        }
    }
}
