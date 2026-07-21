using UnityEditor;

namespace ClaudeCode.Editor.Core
{
    /// <summary>
    /// Manages the reasoning-effort level passed to the CLI via --effort.
    /// "default" means the flag is omitted and the CLI/account default applies.
    /// Valid CLI values: low, medium, high, xhigh, max.
    /// </summary>
    public static class EffortManager
    {
        const string PrefKey = "ClaudeCode_Effort";
        public const string Default = "default";

        // Slider order: index 0 = fastest, last = smartest.
        public static readonly string[] Levels = { "low", "medium", "high", "xhigh", "max" };

        public static string Current
        {
            get => EditorPrefs.GetString(PrefKey, Default);
            set => EditorPrefs.SetString(PrefKey, value);
        }

        public static bool IsDefault => Current == Default;

        /// <summary>Index into Levels for the slider. Default maps to "high" position.</summary>
        public static int CurrentIndex
        {
            get
            {
                var cur = Current;
                for (int i = 0; i < Levels.Length; i++)
                    if (Levels[i] == cur) return i;
                return 2; // default → show at "high"
            }
        }

        public static string DisplayName(string level) => level switch
        {
            "low"    => "낮음",
            "medium" => "중간",
            "high"   => "높음",
            "xhigh"  => "매우 높음",
            "max"    => "최대",
            _        => "기본"
        };

        public static string CurrentDisplayName => DisplayName(Current);
    }
}
