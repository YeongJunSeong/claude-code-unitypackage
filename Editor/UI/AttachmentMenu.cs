using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.Context;

namespace ClaudeCode.Editor.UI
{
    public static class AttachmentMenu
    {
        public class MenuItem
        {
            public IconType Icon;
            public string Label;
            public Action OnClick;
        }

        public static VisualElement Build(EditorWindow ownerWindow, Action<string> onAttach)
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;

            overlay.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.target == overlay)
                    overlay.RemoveFromHierarchy();
            });

            var menu = new VisualElement();
            menu.style.position = Position.Absolute;
            menu.style.bottom = 70;
            menu.style.left = 14;
            menu.style.width = 220;
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

            var items = new List<MenuItem>
            {
                new MenuItem { Icon = IconType.File,       Label = "파일 추가", OnClick = () => { overlay.RemoveFromHierarchy(); AttachFile(onAttach); } },
                new MenuItem { Icon = IconType.Folder,     Label = "폴더 추가", OnClick = () => { overlay.RemoveFromHierarchy(); AttachFolder(onAttach); } },
                new MenuItem { Icon = IconType.GameObject, Label = "선택된 GameObject", OnClick = () => { overlay.RemoveFromHierarchy(); AttachSelectedGameObject(onAttach); } },
                new MenuItem { Icon = IconType.Error,      Label = "콘솔 에러", OnClick = () => { overlay.RemoveFromHierarchy(); AttachConsoleErrors(onAttach); } },
                new MenuItem { Icon = IconType.ChevronDown,Label = "슬래시 명령어", OnClick = () => { overlay.RemoveFromHierarchy(); ShowSlashCommands(ownerWindow, onAttach); } },
            };

            foreach (var item in items)
                menu.Add(BuildMenuItem(item));

            overlay.Add(menu);
            return overlay;
        }

        static VisualElement BuildMenuItem(MenuItem item)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.paddingTop = 8;
            row.style.paddingBottom = 8;
            row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = new Color(0.25f, 0.25f, 0.3f));
            row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = new Color(0, 0, 0, 0));
            row.RegisterCallback<MouseDownEvent>(_ => item.OnClick?.Invoke());

            var iconEl = VectorIcons.Make(item.Icon, 14);
            iconEl.style.marginRight = 8;
            row.Add(iconEl);

            var textLabel = new Label(item.Label);
            textLabel.style.fontSize = 12;
            textLabel.style.color = new Color(0.92f, 0.92f, 0.92f);
            textLabel.style.flexGrow = 1;
            row.Add(textLabel);

            return row;
        }

        static void AttachFile(Action<string> onAttach)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var path = EditorUtility.OpenFilePanel("Attach file", projectRoot, "");
            if (string.IsNullOrEmpty(path)) return;

            var rel = MakeRelativeToProject(path);
            onAttach?.Invoke($"@{rel}");
        }

        static void AttachFolder(Action<string> onAttach)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var path = EditorUtility.OpenFolderPanel("Attach folder", projectRoot, "");
            if (string.IsNullOrEmpty(path)) return;

            var rel = MakeRelativeToProject(path);
            onAttach?.Invoke($"@{rel}");
        }

        static string MakeRelativeToProject(string fullPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace("\\", "/");
            var normalized = fullPath.Replace("\\", "/");
            if (!string.IsNullOrEmpty(projectRoot) && normalized.StartsWith(projectRoot))
                normalized = normalized.Substring(projectRoot.Length).TrimStart('/');
            return normalized;
        }

        static void AttachSelectedGameObject(Action<string> onAttach)
        {
            var selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                EditorUtility.DisplayDialog("Claude Code", "선택된 GameObject가 없습니다.", "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("\n[Attached: Selected GameObjects]");
            foreach (var go in selection)
            {
                sb.AppendLine($"- {GetPath(go)} (active={go.activeSelf})");
                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    sb.AppendLine($"    Component: {comp.GetType().Name}");
                }
            }
            onAttach?.Invoke(sb.ToString());
        }

        static string GetPath(GameObject go)
        {
            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        static void AttachConsoleErrors(Action<string> onAttach)
        {
            var errors = ConsoleLogProvider.GetRecentErrors(10);
            onAttach?.Invoke("\n" + errors);
        }

        static void ShowSlashCommands(EditorWindow ownerWindow, Action<string> onAttach)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("/clear (대화 초기화)"), false, () => onAttach?.Invoke("/clear"));
            menu.AddItem(new GUIContent("/help"), false, () => onAttach?.Invoke("/help"));
            menu.AddItem(new GUIContent("/login"), false, () => onAttach?.Invoke("/login"));
            menu.AddItem(new GUIContent("/logout"), false, () => onAttach?.Invoke("/logout"));
            menu.ShowAsContext();
        }
    }
}
