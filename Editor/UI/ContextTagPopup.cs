using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using ClaudeCode.Editor.Context;

namespace ClaudeCode.Editor.UI
{
    public class ContextTagItem
    {
        public AttachmentKind Kind;
        public string Identifier;
        public string DisplayName;
        public string SubText;
        public IconType Icon;
    }

    public class ContextTagPopup
    {
        readonly VisualElement _root;
        readonly Action<ContextTagItem> _onSelect;
        VisualElement _container;
        List<ContextTagItem> _filtered = new List<ContextTagItem>();
        int _selectedIndex;

        public bool IsOpen => _container != null && _container.parent != null;
        public ContextTagItem SelectedItem => _filtered.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _filtered.Count
            ? _filtered[_selectedIndex] : null;

        public ContextTagPopup(VisualElement root, Action<ContextTagItem> onSelect)
        {
            _root = root;
            _onSelect = onSelect;
        }

        public void Open()
        {
            if (IsOpen) return;
            _container = BuildContainer();
            _root.Add(_container);
            _selectedIndex = 0;
            UpdateFilter("");
        }

        public void Close()
        {
            if (_container != null)
            {
                _container.RemoveFromHierarchy();
                _container = null;
            }
        }

        public void UpdateFilter(string query)
        {
            _filtered = BuildItems(query);
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _filtered.Count - 1));
            Render();
        }

        public bool HandleKey(KeyDownEvent evt)
        {
            if (!IsOpen) return false;

            if (evt.keyCode == KeyCode.DownArrow)
            {
                if (_filtered.Count > 0)
                {
                    _selectedIndex = (_selectedIndex + 1) % _filtered.Count;
                    Render();
                }
                return true;
            }

            if (evt.keyCode == KeyCode.UpArrow)
            {
                if (_filtered.Count > 0)
                {
                    _selectedIndex = (_selectedIndex - 1 + _filtered.Count) % _filtered.Count;
                    Render();
                }
                return true;
            }

            if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                return true;
            }

            if (evt.keyCode == KeyCode.Tab || (evt.keyCode == KeyCode.Return && !evt.shiftKey))
            {
                var item = SelectedItem;
                if (item != null)
                {
                    _onSelect?.Invoke(item);
                    Close();
                }
                return true;
            }

            return false;
        }

        VisualElement BuildContainer()
        {
            var c = new VisualElement();
            c.style.position = Position.Absolute;
            c.style.left = 14;
            c.style.bottom = 70;
            c.style.width = 380;
            c.style.maxHeight = 320;
            c.style.backgroundColor = new Color(0.16f, 0.16f, 0.18f);
            c.style.borderTopLeftRadius = 8;
            c.style.borderTopRightRadius = 8;
            c.style.borderBottomLeftRadius = 8;
            c.style.borderBottomRightRadius = 8;
            c.style.paddingTop = 4;
            c.style.paddingBottom = 4;
            c.style.borderTopWidth = 1;
            c.style.borderBottomWidth = 1;
            c.style.borderLeftWidth = 1;
            c.style.borderRightWidth = 1;
            c.style.borderTopColor = new Color(0.34f, 0.34f, 0.38f);
            c.style.borderBottomColor = new Color(0.34f, 0.34f, 0.38f);
            c.style.borderLeftColor = new Color(0.34f, 0.34f, 0.38f);
            c.style.borderRightColor = new Color(0.34f, 0.34f, 0.38f);
            c.pickingMode = PickingMode.Ignore;

            var header = new Label("컨텍스트 첨부 (↑↓ 이동, Enter/Tab 선택, Esc 닫기)");
            header.style.fontSize = 9;
            header.style.color = new Color(0.5f, 0.5f, 0.55f);
            header.style.paddingLeft = 10;
            header.style.paddingRight = 10;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 6;
            c.Add(header);

            var list = new ScrollView(ScrollViewMode.Vertical);
            list.style.flexGrow = 1;
            list.name = "ctx-list";
            c.Add(list);

            return c;
        }

        void Render()
        {
            if (_container == null) return;
            var list = _container.Q<ScrollView>("ctx-list");
            if (list == null) return;
            list.Clear();

            if (_filtered.Count == 0)
            {
                var empty = new Label("일치하는 항목 없음");
                empty.style.color = new Color(0.5f, 0.5f, 0.55f);
                empty.style.fontSize = 11;
                empty.style.paddingLeft = 12;
                empty.style.paddingTop = 8;
                empty.style.paddingBottom = 8;
                list.Add(empty);
                return;
            }

            string lastSection = null;
            for (int i = 0; i < _filtered.Count; i++)
            {
                var item = _filtered[i];
                var section = SectionName(item.Kind);
                if (section != lastSection)
                {
                    list.Add(BuildSectionHeader(section));
                    lastSection = section;
                }
                list.Add(BuildItem(item, i == _selectedIndex));
            }
        }

        static string SectionName(AttachmentKind kind) => kind switch
        {
            AttachmentKind.GameObject => "GameObjects",
            AttachmentKind.Scene => "Scenes",
            AttachmentKind.Selection => "Special",
            AttachmentKind.ConsoleErrors => "Special",
            AttachmentKind.ProjectStructure => "Special",
            AttachmentKind.Folder => "Folders",
            _ => "Files"
        };

        VisualElement BuildSectionHeader(string name)
        {
            var hdr = new Label(name);
            hdr.style.fontSize = 9;
            hdr.style.color = new Color(0.55f, 0.7f, 0.95f);
            hdr.style.unityFontStyleAndWeight = FontStyle.Bold;
            hdr.style.paddingLeft = 10;
            hdr.style.paddingRight = 10;
            hdr.style.paddingTop = 6;
            hdr.style.paddingBottom = 2;
            return hdr;
        }

        VisualElement BuildItem(ContextTagItem item, bool isSelected)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 10;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;
            row.style.backgroundColor = isSelected
                ? new Color(0.25f, 0.32f, 0.5f)
                : new Color(0, 0, 0, 0);

            row.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (!isSelected) row.style.backgroundColor = new Color(0.22f, 0.22f, 0.26f);
            });
            row.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (!isSelected) row.style.backgroundColor = new Color(0, 0, 0, 0);
            });
            row.RegisterCallback<MouseDownEvent>(_ => { _onSelect?.Invoke(item); Close(); });

            var icon = VectorIcons.Make(item.Icon, 14);
            icon.style.marginLeft = 4;
            icon.style.marginRight = 8;
            row.Add(icon);

            var textBox = new VisualElement();
            textBox.style.flexGrow = 1;
            textBox.style.flexShrink = 1;
            textBox.style.overflow = Overflow.Hidden;

            var name = new Label(item.DisplayName);
            name.style.color = isSelected ? Color.white : new Color(0.92f, 0.92f, 0.92f);
            name.style.fontSize = 12;
            name.style.whiteSpace = WhiteSpace.NoWrap;
            name.style.overflow = Overflow.Hidden;
            name.style.textOverflow = TextOverflow.Ellipsis;
            textBox.Add(name);

            if (!string.IsNullOrEmpty(item.SubText))
            {
                var sub = new Label(item.SubText);
                sub.style.color = new Color(0.55f, 0.55f, 0.6f);
                sub.style.fontSize = 9;
                sub.style.whiteSpace = WhiteSpace.NoWrap;
                sub.style.overflow = Overflow.Hidden;
                sub.style.textOverflow = TextOverflow.Ellipsis;
                textBox.Add(sub);
            }

            row.Add(textBox);
            return row;
        }

        // ---- Item collection ----

        List<ContextTagItem> BuildItems(string query)
        {
            var result = new List<ContextTagItem>();
            var q = (query ?? "").ToLowerInvariant();

            AddSpecialItems(result, q);
            AddSceneObjects(result, q);
            AddAssets(result, q);

            return result;
        }

        void AddSpecialItems(List<ContextTagItem> result, string q)
        {
            var specials = new[]
            {
                new ContextTagItem { Kind = AttachmentKind.Selection, Identifier = "Selection", DisplayName = "Selection", SubText = "현재 선택된 GameObject들", Icon = IconType.GameObject },
                new ContextTagItem { Kind = AttachmentKind.Scene, Identifier = "ActiveScene", DisplayName = "ActiveScene", SubText = "현재 활성 씬 정보", Icon = IconType.Scene },
                new ContextTagItem { Kind = AttachmentKind.ConsoleErrors, Identifier = "ConsoleErrors", DisplayName = "ConsoleErrors", SubText = "최근 에러/경고 10개", Icon = IconType.Error },
                new ContextTagItem { Kind = AttachmentKind.ProjectStructure, Identifier = "ProjectStructure", DisplayName = "ProjectStructure", SubText = "프로젝트 디렉토리 구조", Icon = IconType.Search },
            };

            foreach (var s in specials)
                if (string.IsNullOrEmpty(q) || s.DisplayName.ToLowerInvariant().Contains(q))
                    result.Add(s);
        }

        void AddSceneObjects(List<ContextTagItem> result, string q)
        {
            const int Max = 30;
            int added = 0;

            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (added >= Max) break;
                added = AddGameObjectRecursive(result, root, q, added, Max);
            }
        }

        int AddGameObjectRecursive(List<ContextTagItem> result, GameObject go, string q, int added, int max)
        {
            if (added >= max) return added;
            var path = GetGameObjectPath(go);
            if (string.IsNullOrEmpty(q) || go.name.ToLowerInvariant().Contains(q) || path.ToLowerInvariant().Contains(q))
            {
                result.Add(new ContextTagItem
                {
                    Kind = AttachmentKind.GameObject,
                    Identifier = path,
                    DisplayName = go.name,
                    SubText = path,
                    Icon = IconType.GameObject
                });
                added++;
            }

            for (int i = 0; i < go.transform.childCount && added < max; i++)
                added = AddGameObjectRecursive(result, go.transform.GetChild(i).gameObject, q, added, max);

            return added;
        }

        static string GetGameObjectPath(GameObject go)
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

        void AddAssets(List<ContextTagItem> result, string q)
        {
            const int Max = 40;
            string filter = string.IsNullOrEmpty(q) ? "t:Script t:Prefab" : q;
            var guids = AssetDatabase.FindAssets(filter);

            int added = 0;
            foreach (var guid in guids)
            {
                if (added >= Max) break;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(Application.dataPath) + "/" + path) && string.IsNullOrEmpty(q))
                    continue;

                var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                AttachmentKind kind = ext switch
                {
                    ".cs" => AttachmentKind.Script,
                    ".prefab" => AttachmentKind.Prefab,
                    ".mat" or ".shader" or ".shadergraph" => AttachmentKind.Material,
                    ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" => AttachmentKind.Image,
                    _ => AttachmentKind.File
                };
                IconType icon = kind switch
                {
                    AttachmentKind.Script => IconType.Script,
                    AttachmentKind.Prefab => IconType.Prefab,
                    AttachmentKind.Material => IconType.Material,
                    AttachmentKind.Image => IconType.Image,
                    _ => IconType.File
                };

                result.Add(new ContextTagItem
                {
                    Kind = kind,
                    Identifier = path,
                    DisplayName = System.IO.Path.GetFileName(path),
                    SubText = path,
                    Icon = icon
                });
                added++;
            }
        }
    }
}
