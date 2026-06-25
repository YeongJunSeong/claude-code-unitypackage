using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClaudeCode.Editor.UI
{
    public static class MarkdownRenderer
    {
        static readonly Regex CodeBlockRegex = new Regex(@"```(\w*)\r?\n([\s\S]*?)```", RegexOptions.Compiled);
        static readonly Regex InlineCodeRegex = new Regex(@"`([^`\n]+)`", RegexOptions.Compiled);
        static readonly Regex BoldRegex = new Regex(@"\*\*([^*\n]+)\*\*", RegexOptions.Compiled);
        static readonly Regex ItalicRegex = new Regex(@"(?<!\*)\*(?!\*)([^*\n]+?)(?<!\*)\*(?!\*)", RegexOptions.Compiled);
        static readonly Regex LinkRegex = new Regex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
        static readonly Regex HeadingRegex = new Regex(@"^(#{1,6})\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex ListItemRegex = new Regex(@"^\s*([-*+]|\d+\.)\s+(.+)$", RegexOptions.Compiled);

        public static void Render(string markdown, VisualElement container)
        {
            container.Clear();
            if (string.IsNullOrEmpty(markdown))
                return;

            var blocks = SplitIntoBlocks(markdown);

            // 연속된 텍스트 블록(단락/제목/리스트)은 하나의 선택 가능한 Label로 합쳐서
            // 블록 경계에서 드래그 선택이 끊기지 않게 한다. 코드 블록만 별도 박스로 분리.
            var proseRun = new List<Block>();
            foreach (var block in blocks)
            {
                if (block.Type == "code")
                {
                    FlushProseRun(proseRun, container);
                    var codeEl = RenderCodeBlock(block);
                    if (codeEl != null) container.Add(codeEl);
                }
                else
                {
                    proseRun.Add(block);
                }
            }
            FlushProseRun(proseRun, container);
        }

        static void FlushProseRun(List<Block> run, VisualElement container)
        {
            if (run.Count == 0) return;
            var label = RenderProseRun(run);
            if (label != null) container.Add(label);
            run.Clear();
        }

        static VisualElement RenderProseRun(List<Block> blocks)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (i > 0)
                    sb.Append(Separator(blocks[i - 1].Type, b.Type));

                switch (b.Type)
                {
                    case "heading":
                        int.TryParse(b.Language, out int level);
                        int size = level switch { 1 => 17, 2 => 15, 3 => 14, _ => 13 };
                        sb.Append($"<size={size}><b><color=#FFFFFF>{ApplyInlineFormatting(b.Content)}</color></b></size>");
                        break;
                    case "list":
                        sb.Append("<color=#B3B3B3>•</color> ");
                        sb.Append(ApplyInlineFormatting(b.Content));
                        break;
                    default: // paragraph
                        sb.Append(ApplyInlineFormatting(b.Content));
                        break;
                }
            }

            var label = new Label(sb.ToString());
            label.enableRichText = true;
            label.style.fontSize = 13;
            label.style.color = new Color(0.9f, 0.9f, 0.9f);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 2;
            label.style.marginBottom = 2;
            label.selection.isSelectable = true;
            return label;
        }

        // 블록 간 줄간격: 리스트끼리는 한 줄, 그 외 단락/제목 전환은 빈 줄 하나.
        static string Separator(string prevType, string curType)
        {
            if (curType == "heading") return "\n\n";
            if (prevType == "heading") return "\n";
            if (prevType == "list" || curType == "list") return "\n";
            return "\n\n";
        }

        struct Block
        {
            public string Type;
            public string Content;
            public string Language;
        }

        static List<Block> SplitIntoBlocks(string md)
        {
            var blocks = new List<Block>();
            int pos = 0;

            while (pos < md.Length)
            {
                var codeMatch = CodeBlockRegex.Match(md, pos);
                if (codeMatch.Success)
                {
                    if (codeMatch.Index > pos)
                    {
                        var textPart = md.Substring(pos, codeMatch.Index - pos);
                        AppendTextBlocks(textPart, blocks);
                    }
                    blocks.Add(new Block
                    {
                        Type = "code",
                        Language = codeMatch.Groups[1].Value,
                        Content = codeMatch.Groups[2].Value.TrimEnd('\r', '\n')
                    });
                    pos = codeMatch.Index + codeMatch.Length;
                }
                else
                {
                    AppendTextBlocks(md.Substring(pos), blocks);
                    break;
                }
            }

            return blocks;
        }

        static void AppendTextBlocks(string text, List<Block> blocks)
        {
            var lines = text.Split('\n');
            var current = new StringBuilder();

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');

                if (string.IsNullOrWhiteSpace(line))
                {
                    Flush(current, blocks);
                    continue;
                }

                if (HeadingRegex.IsMatch(line))
                {
                    Flush(current, blocks);
                    var hm = HeadingRegex.Match(line);
                    blocks.Add(new Block
                    {
                        Type = "heading",
                        Language = hm.Groups[1].Value.Length.ToString(),
                        Content = hm.Groups[2].Value
                    });
                    continue;
                }

                if (ListItemRegex.IsMatch(line))
                {
                    Flush(current, blocks);
                    blocks.Add(new Block
                    {
                        Type = "list",
                        Content = ListItemRegex.Match(line).Groups[2].Value
                    });
                    continue;
                }

                if (current.Length > 0) current.Append('\n');
                current.Append(line);
            }

            Flush(current, blocks);
        }

        static void Flush(StringBuilder sb, List<Block> blocks)
        {
            if (sb.Length == 0) return;
            blocks.Add(new Block { Type = "paragraph", Content = sb.ToString() });
            sb.Clear();
        }

        static VisualElement RenderCodeBlock(Block block)
        {
            var wrap = new VisualElement();
            wrap.style.marginTop = 6;
            wrap.style.marginBottom = 6;
            wrap.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            wrap.style.borderTopLeftRadius = 6;
            wrap.style.borderTopRightRadius = 6;
            wrap.style.borderBottomLeftRadius = 6;
            wrap.style.borderBottomRightRadius = 6;
            wrap.style.borderTopWidth = 1;
            wrap.style.borderBottomWidth = 1;
            wrap.style.borderLeftWidth = 1;
            wrap.style.borderRightWidth = 1;
            wrap.style.borderTopColor = new Color(0.25f, 0.25f, 0.28f);
            wrap.style.borderBottomColor = new Color(0.25f, 0.25f, 0.28f);
            wrap.style.borderLeftColor = new Color(0.25f, 0.25f, 0.28f);
            wrap.style.borderRightColor = new Color(0.25f, 0.25f, 0.28f);

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.paddingLeft = 10;
            headerRow.style.paddingRight = 6;
            headerRow.style.paddingTop = 4;
            headerRow.style.paddingBottom = 2;
            headerRow.style.minHeight = 22;

            var langLabel = new Label(string.IsNullOrEmpty(block.Language) ? "code" : block.Language);
            langLabel.style.fontSize = 9;
            langLabel.style.color = new Color(0.55f, 0.7f, 0.95f);
            langLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerRow.Add(langLabel);

            var codeText = block.Content;
            var copyBtn = new Button(() =>
            {
                GUIUtility.systemCopyBuffer = codeText;
            }) { text = "Copy" };
            copyBtn.style.fontSize = 9;
            copyBtn.style.height = 18;
            copyBtn.style.paddingLeft = 6;
            copyBtn.style.paddingRight = 6;
            copyBtn.style.marginLeft = 4;
            copyBtn.style.backgroundColor = new Color(0.22f, 0.22f, 0.26f);
            copyBtn.style.color = new Color(0.8f, 0.85f, 0.9f);
            copyBtn.style.borderTopLeftRadius = 3;
            copyBtn.style.borderTopRightRadius = 3;
            copyBtn.style.borderBottomLeftRadius = 3;
            copyBtn.style.borderBottomRightRadius = 3;
            copyBtn.clicked += () =>
            {
                copyBtn.text = "Copied";
                copyBtn.schedule.Execute(() => copyBtn.text = "Copy").StartingIn(1200);
            };
            headerRow.Add(copyBtn);

            wrap.Add(headerRow);

            var highlighted = CodeHighlighter.Highlight(block.Content, block.Language);
            var displayText = highlighted ?? block.Content;
            var isHighlighted = highlighted != null;

            var code = new Label(displayText);
            code.enableRichText = true;
            code.style.fontSize = 11;
            code.style.color = isHighlighted
                ? new Color(0.83f, 0.83f, 0.83f)  // VS Dark default
                : new Color(0.85f, 0.9f, 0.85f);  // Fallback non-highlighted color
            code.style.paddingLeft = 10;
            code.style.paddingRight = 10;
            code.style.paddingTop = 6;
            code.style.paddingBottom = 6;
            code.style.whiteSpace = WhiteSpace.Normal;
            code.selection.isSelectable = true;
            wrap.Add(code);

            return wrap;
        }

        static string ApplyInlineFormatting(string text)
        {
            text = text.Replace("<", "&lt;").Replace(">", "&gt;");

            text = LinkRegex.Replace(text, m =>
                $"<color=#7AB8FF><u>{m.Groups[1].Value}</u></color>");

            text = BoldRegex.Replace(text, "<b>$1</b>");
            text = ItalicRegex.Replace(text, "<i>$1</i>");

            text = InlineCodeRegex.Replace(text, m =>
                $"<color=#E0C080><noparse>{m.Groups[1].Value}</noparse></color>");

            return text;
        }
    }
}
