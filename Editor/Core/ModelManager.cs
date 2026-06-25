using System.Collections.Generic;
using UnityEditor;

namespace ClaudeCode.Editor.Core
{
    public static class ModelManager
    {
        const string PrefKey = "ClaudeCode_Model";
        const string DefaultModel = "default";

        public class ModelOption
        {
            public string DisplayName;
            public string CliValue;
        }

        public static readonly List<ModelOption> Options = new List<ModelOption>
        {
            new ModelOption { DisplayName = "Default",        CliValue = "default" },
            new ModelOption { DisplayName = "Opus",           CliValue = "opus" },
            new ModelOption { DisplayName = "Sonnet",         CliValue = "sonnet" },
            new ModelOption { DisplayName = "Haiku",          CliValue = "haiku" },
            new ModelOption { DisplayName = "Opus 4.8",       CliValue = "claude-opus-4-8" },
            new ModelOption { DisplayName = "Opus 4.7",       CliValue = "claude-opus-4-7" },
            new ModelOption { DisplayName = "Opus 4.6",       CliValue = "claude-opus-4-6" },
            new ModelOption { DisplayName = "Sonnet 4.6",     CliValue = "claude-sonnet-4-6" },
            new ModelOption { DisplayName = "Haiku 4.5",      CliValue = "claude-haiku-4-5-20251001" },
        };

        public static string CurrentModel
        {
            get => EditorPrefs.GetString(PrefKey, DefaultModel);
            set => EditorPrefs.SetString(PrefKey, value);
        }

        public static string CurrentDisplayName
        {
            get
            {
                var current = CurrentModel;
                var opt = Options.Find(o => o.CliValue == current);
                return opt?.DisplayName ?? current;
            }
        }

        public static List<string> GetDisplayNames()
        {
            var list = new List<string>();
            foreach (var o in Options) list.Add(o.DisplayName);
            return list;
        }

        public static void SetByDisplayName(string displayName)
        {
            var opt = Options.Find(o => o.DisplayName == displayName);
            if (opt != null) CurrentModel = opt.CliValue;
        }

        public static int GetCurrentIndex()
        {
            var current = CurrentModel;
            return Options.FindIndex(o => o.CliValue == current);
        }
    }
}
