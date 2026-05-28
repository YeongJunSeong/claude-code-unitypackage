using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClaudeCode.Editor.UI
{
    public enum AttachmentKind
    {
        File,
        Folder,
        Script,
        Prefab,
        Material,
        Image,
        GameObject,
        Scene,
        Selection,
        ConsoleErrors,
        ProjectStructure
    }

    public class Attachment
    {
        public AttachmentKind Kind;
        public string Identifier;
        public string DisplayName;
        public Texture2D Thumbnail;
        public string Snippet;

        public string Icon => Kind switch
        {
            AttachmentKind.File => "F",
            AttachmentKind.Folder => "D",
            AttachmentKind.Script => "CS",
            AttachmentKind.Prefab => "P",
            AttachmentKind.Material => "M",
            AttachmentKind.Image => "I",
            AttachmentKind.GameObject => "GO",
            AttachmentKind.Scene => "S",
            AttachmentKind.Selection => "Sel",
            AttachmentKind.ConsoleErrors => "Err",
            AttachmentKind.ProjectStructure => "PS",
            _ => "•"
        };

        public IconType? VectorIcon
        {
            get
            {
                switch (Kind)
                {
                    case AttachmentKind.File: return IconType.File;
                    case AttachmentKind.Folder: return IconType.Folder;
                    case AttachmentKind.Script: return IconType.Script;
                    case AttachmentKind.Prefab: return IconType.Prefab;
                    case AttachmentKind.Material: return IconType.Material;
                    case AttachmentKind.Image: return IconType.Image;
                    case AttachmentKind.GameObject: return IconType.GameObject;
                    case AttachmentKind.Scene: return IconType.Scene;
                    case AttachmentKind.ConsoleErrors: return IconType.Error;
                    case AttachmentKind.ProjectStructure: return IconType.Search;
                    case AttachmentKind.Selection: return IconType.GameObject;
                    default: return null;
                }
            }
        }
    }

    public class AttachmentBar
    {
        readonly List<Attachment> _items = new List<Attachment>();
        VisualElement _container;

        public IReadOnlyList<Attachment> Items => _items;

        public VisualElement Build()
        {
            _container = new VisualElement();
            _container.style.flexDirection = FlexDirection.Row;
            _container.style.flexWrap = Wrap.Wrap;
            _container.style.paddingTop = 4;
            _container.style.paddingBottom = 4;
            _container.style.display = DisplayStyle.None;
            return _container;
        }

        public void Add(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            AddInternal(new Attachment
            {
                Kind = DetectKind(path),
                Identifier = path,
                DisplayName = System.IO.Path.GetFileName(path),
                Thumbnail = IsImageFile(path) ? LoadThumbnail(path) : null
            });
        }

        public void AddAttachment(Attachment att)
        {
            if (att == null) return;
            AddInternal(att);
        }

        void AddInternal(Attachment att)
        {
            if (_items.Exists(a => a.Kind == att.Kind && a.Identifier == att.Identifier)) return;
            _items.Add(att);
            Render();
        }

        public void Remove(Attachment att)
        {
            _items.Remove(att);
            Render();
        }

        public void Clear()
        {
            _items.Clear();
            Render();
        }

        static AttachmentKind DetectKind(string path)
        {
            var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
            switch (ext)
            {
                case ".cs": return AttachmentKind.Script;
                case ".prefab": return AttachmentKind.Prefab;
                case ".mat":
                case ".shader":
                case ".shadergraph": return AttachmentKind.Material;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".bmp":
                case ".webp": return AttachmentKind.Image;
                default:
                    if (Directory.Exists(MakeAbsolute(path))) return AttachmentKind.Folder;
                    return AttachmentKind.File;
            }
        }

        static string MakeAbsolute(string path)
        {
            if (Path.IsPathRooted(path)) return path;
            var root = Path.GetDirectoryName(Application.dataPath);
            return string.IsNullOrEmpty(root) ? path : Path.Combine(root, path);
        }

        void Render()
        {
            if (_container == null) return;
            _container.Clear();

            _container.style.display = _items.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            foreach (var att in _items)
                _container.Add(BuildChip(att));
        }

        VisualElement BuildChip(Attachment att)
        {
            var chip = new VisualElement();
            chip.style.flexDirection = FlexDirection.Row;
            chip.style.alignItems = Align.Center;
            chip.style.backgroundColor = ChipBgFor(att.Kind);
            chip.style.borderTopLeftRadius = 6;
            chip.style.borderTopRightRadius = 6;
            chip.style.borderBottomLeftRadius = 6;
            chip.style.borderBottomRightRadius = 6;
            chip.style.paddingLeft = 6;
            chip.style.paddingRight = 6;
            chip.style.paddingTop = 4;
            chip.style.paddingBottom = 4;
            chip.style.marginRight = 6;
            chip.style.marginBottom = 4;
            chip.style.borderTopWidth = 1;
            chip.style.borderBottomWidth = 1;
            chip.style.borderLeftWidth = 1;
            chip.style.borderRightWidth = 1;
            var border = ChipBorderFor(att.Kind);
            chip.style.borderTopColor = border;
            chip.style.borderBottomColor = border;
            chip.style.borderLeftColor = border;
            chip.style.borderRightColor = border;

            if (att.Kind == AttachmentKind.Image && att.Thumbnail != null)
            {
                var img = new Image { image = att.Thumbnail, scaleMode = ScaleMode.ScaleToFit };
                img.style.width = 48;
                img.style.height = 48;
                img.style.marginRight = 6;
                img.style.borderTopLeftRadius = 4;
                img.style.borderTopRightRadius = 4;
                img.style.borderBottomLeftRadius = 4;
                img.style.borderBottomRightRadius = 4;
                chip.Add(img);
            }
            else if (att.VectorIcon.HasValue)
            {
                var iconImg = VectorIcons.Make(att.VectorIcon.Value, 14);
                iconImg.style.marginRight = 6;
                chip.Add(iconImg);
            }
            else
            {
                var icon = new Label(att.Icon);
                icon.style.fontSize = 11;
                icon.style.unityFontStyleAndWeight = FontStyle.Bold;
                icon.style.color = new Color(0.7f, 0.7f, 0.75f);
                icon.style.marginRight = 6;
                chip.Add(icon);
            }

            var label = new Label(Truncate(att.DisplayName, 32));
            label.style.color = new Color(0.92f, 0.92f, 0.92f);
            label.style.fontSize = 11;
            label.style.marginRight = 8;
            chip.Add(label);

            var removeBtn = new Button(() => Remove(att)) { text = "" };
            removeBtn.style.width = 18;
            removeBtn.style.height = 18;
            removeBtn.style.paddingLeft = 0;
            removeBtn.style.paddingRight = 0;
            removeBtn.style.paddingTop = 0;
            removeBtn.style.paddingBottom = 0;
            removeBtn.style.backgroundColor = new Color(0.4f, 0.2f, 0.2f);
            removeBtn.style.alignItems = Align.Center;
            removeBtn.style.justifyContent = Justify.Center;
            removeBtn.style.borderTopLeftRadius = 9;
            removeBtn.style.borderTopRightRadius = 9;
            removeBtn.style.borderBottomLeftRadius = 9;
            removeBtn.style.borderBottomRightRadius = 9;
            var rmIcon = VectorIcons.Make(IconType.Close, 10, Color.white);
            removeBtn.Add(rmIcon);
            chip.Add(removeBtn);

            return chip;
        }

        static Color ChipBgFor(AttachmentKind kind) => kind switch
        {
            AttachmentKind.Script => new Color(0.20f, 0.30f, 0.25f),
            AttachmentKind.Prefab => new Color(0.28f, 0.22f, 0.32f),
            AttachmentKind.Material => new Color(0.32f, 0.28f, 0.20f),
            AttachmentKind.Image => new Color(0.22f, 0.22f, 0.26f),
            AttachmentKind.GameObject => new Color(0.22f, 0.30f, 0.40f),
            AttachmentKind.Scene => new Color(0.30f, 0.24f, 0.40f),
            AttachmentKind.Selection => new Color(0.34f, 0.30f, 0.22f),
            AttachmentKind.ConsoleErrors => new Color(0.40f, 0.20f, 0.20f),
            AttachmentKind.ProjectStructure => new Color(0.22f, 0.28f, 0.32f),
            AttachmentKind.Folder => new Color(0.28f, 0.26f, 0.20f),
            _ => new Color(0.22f, 0.22f, 0.26f)
        };

        static Color ChipBorderFor(AttachmentKind kind)
        {
            var bg = ChipBgFor(kind);
            return new Color(bg.r + 0.12f, bg.g + 0.12f, bg.b + 0.12f);
        }

        static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max - 1) + "…";
        }

        static bool IsImageFile(string path)
        {
            var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp" || ext == ".webp";
        }

        static Texture2D LoadThumbnail(string relativePath)
        {
            try
            {
                var projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
                var fullPath = System.IO.Path.IsPathRooted(relativePath)
                    ? relativePath
                    : System.IO.Path.Combine(projectRoot, relativePath);

                if (!File.Exists(fullPath)) return null;

                var bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes)) return tex;
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
