using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClaudeCode.Editor.UI
{
    public class SlashCommandPopup
    {
        readonly VisualElement _root;
        readonly Action<SlashCommand> _onSelect;
        VisualElement _container;
        List<SlashCommand> _filtered = new List<SlashCommand>();
        int _selectedIndex;

        public bool IsOpen => _container != null && _container.parent != null;
        public SlashCommand SelectedCommand => _filtered.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _filtered.Count
            ? _filtered[_selectedIndex] : null;

        public SlashCommandPopup(VisualElement root, Action<SlashCommand> onSelect)
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
            _filtered = SlashCommandRegistry.Filter(query);
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

            if (evt.keyCode == KeyCode.Tab)
            {
                var cmd = SelectedCommand;
                if (cmd != null)
                {
                    _onSelect?.Invoke(cmd);
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
            c.style.width = 340;
            c.style.maxHeight = 240;
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

            var header = new Label("슬래시 명령어 (↑↓ 선택, Enter 실행, Tab 채우기, Esc 닫기)");
            header.style.fontSize = 9;
            header.style.color = new Color(0.5f, 0.5f, 0.55f);
            header.style.paddingLeft = 10;
            header.style.paddingRight = 10;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 6;
            c.Add(header);

            var list = new ScrollView(ScrollViewMode.Vertical);
            list.style.flexGrow = 1;
            list.name = "slash-list";
            c.Add(list);

            return c;
        }

        void Render()
        {
            if (_container == null) return;
            var list = _container.Q<ScrollView>("slash-list");
            if (list == null) return;
            list.Clear();

            if (_filtered.Count == 0)
            {
                var empty = new Label("일치하는 명령어 없음");
                empty.style.color = new Color(0.5f, 0.5f, 0.55f);
                empty.style.fontSize = 11;
                empty.style.paddingLeft = 12;
                empty.style.paddingTop = 8;
                empty.style.paddingBottom = 8;
                list.Add(empty);
                return;
            }

            for (int i = 0; i < _filtered.Count; i++)
            {
                var cmd = _filtered[i];
                bool isSelected = i == _selectedIndex;
                list.Add(BuildItem(cmd, isSelected));
            }
        }

        VisualElement BuildItem(SlashCommand cmd, bool isSelected)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
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
            row.RegisterCallback<MouseDownEvent>(_ => _onSelect?.Invoke(cmd));

            var icon = new Label(cmd.Icon);
            icon.style.width = 22;
            icon.style.fontSize = 13;
            row.Add(icon);

            var name = new Label(cmd.Name);
            name.style.color = isSelected ? Color.white : new Color(0.85f, 0.95f, 0.85f);
            name.style.fontSize = 12;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.minWidth = 90;
            name.style.marginRight = 8;
            row.Add(name);

            var desc = new Label(cmd.Description);
            desc.style.color = new Color(0.7f, 0.7f, 0.75f);
            desc.style.fontSize = 11;
            desc.style.flexGrow = 1;
            row.Add(desc);

            return row;
        }
    }
}
