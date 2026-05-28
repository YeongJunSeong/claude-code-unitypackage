using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeCode.Editor.UI
{
    /// <summary>
    /// Lightweight Visual Studio (Dark+) inspired C# syntax highlighter.
    /// Converts raw C# code to a Unity rich-text-tagged string for display in Label.
    /// </summary>
    public static class CSharpHighlighter
    {
        // VS Dark+ inspired colors
        const string KeywordColor   = "#569CD6"; // public, class, void
        const string ControlColor   = "#C586C0"; // if, return, for
        const string TypeColor      = "#4EC9B0"; // string, int, GameObject
        const string MethodColor    = "#DCDCAA"; // method calls
        const string StringColor    = "#CE9178"; // "..."
        const string NumberColor    = "#B5CEA8"; // 42, 1.0f
        const string CommentColor   = "#6A9955"; // // ..., /* ... */
        const string AttributeColor = "#DCDCAA"; // [SerializeField]
        const string PreprocColor   = "#BD63C5"; // #if, #endif

        static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "public", "private", "protected", "internal", "static", "readonly", "const",
            "abstract", "virtual", "override", "sealed", "new", "class", "interface",
            "struct", "enum", "namespace", "using", "partial", "extern", "unsafe",
            "async", "await", "yield", "void", "var", "in", "out", "ref", "params",
            "this", "base", "null", "true", "false", "get", "set", "value", "delegate",
            "event", "operator", "implicit", "explicit", "checked", "unchecked",
            "fixed", "lock", "stackalloc", "typeof", "sizeof", "default", "global",
            "where", "nameof", "record", "init", "with", "and", "or", "not"
        };

        static readonly HashSet<string> ControlKeywords = new HashSet<string>
        {
            "if", "else", "switch", "case", "for", "foreach", "while", "do",
            "break", "continue", "return", "throw", "try", "catch", "finally",
            "goto", "when", "is", "as"
        };

        static readonly HashSet<string> Types = new HashSet<string>
        {
            // primitives
            "int", "long", "short", "byte", "sbyte", "uint", "ulong", "ushort",
            "float", "double", "decimal", "char", "string", "bool", "object", "dynamic", "nint", "nuint",

            // .NET common
            "List", "Dictionary", "HashSet", "Queue", "Stack", "Array", "ArrayList",
            "IEnumerable", "IEnumerator", "IList", "ICollection", "IDictionary",
            "IComparable", "IComparer", "IEquatable", "IDisposable",
            "Action", "Func", "Predicate", "Task", "ValueTask", "Tuple", "ValueTuple",
            "Exception", "ArgumentException", "ArgumentNullException", "InvalidOperationException",
            "EventArgs", "EventHandler", "DateTime", "TimeSpan", "Guid", "Random",
            "StringBuilder", "Regex", "Match", "Encoding",

            // Unity Engine - core
            "GameObject", "MonoBehaviour", "ScriptableObject", "Transform", "RectTransform",
            "Component", "Behaviour", "Object",

            // Physics
            "Rigidbody", "Rigidbody2D", "Collider", "Collider2D", "CharacterController",
            "BoxCollider", "SphereCollider", "CapsuleCollider", "MeshCollider",
            "BoxCollider2D", "CircleCollider2D", "CapsuleCollider2D", "PolygonCollider2D",
            "Joint", "Joint2D", "HingeJoint", "FixedJoint",

            // Math
            "Vector2", "Vector3", "Vector4", "Vector2Int", "Vector3Int",
            "Quaternion", "Matrix4x4", "Bounds", "Rect", "RectInt", "Plane", "Ray", "Ray2D",
            "RaycastHit", "RaycastHit2D",

            // Color & rendering
            "Color", "Color32", "Gradient",
            "Material", "Shader", "Texture", "Texture2D", "Texture3D", "Cubemap", "RenderTexture",
            "Sprite", "SpriteAtlas", "Mesh", "MeshFilter", "MeshRenderer", "SkinnedMeshRenderer",
            "SpriteRenderer", "ParticleSystem", "LineRenderer", "TrailRenderer",
            "Camera", "Light", "LightProbe", "Skybox", "ReflectionProbe",

            // Audio
            "AudioSource", "AudioClip", "AudioListener", "AudioMixer", "AudioMixerGroup",

            // Animation
            "Animator", "Animation", "AnimationClip", "AnimationCurve", "AnimationEvent",
            "AnimatorController", "AnimatorStateInfo",

            // UI
            "Canvas", "CanvasGroup", "GraphicRaycaster",
            "Button", "Image", "RawImage", "Text", "InputField",
            "Toggle", "Slider", "Scrollbar", "Dropdown", "ScrollRect",
            "TMP_Text", "TMP_InputField", "TextMeshProUGUI", "TextMeshPro",

            // Static helpers
            "Time", "Mathf", "Mathd", "Physics", "Physics2D", "Input",
            "Application", "Resources", "PlayerPrefs", "SceneManager", "Scene",
            "Debug", "Gizmos", "Handles", "GUI", "GUILayout", "GUIStyle",

            // Coroutine
            "Coroutine", "WaitForSeconds", "WaitForEndOfFrame", "WaitForFixedUpdate", "WaitUntil", "WaitWhile",

            // Editor
            "Editor", "EditorWindow", "EditorGUI", "EditorGUILayout", "EditorUtility",
            "AssetDatabase", "ScriptableSingleton", "SerializedObject", "SerializedProperty",
            "Selection", "Undo", "PrefabUtility", "MenuItem"
        };

        // Single combined regex with named groups.
        static readonly Regex TokenRegex = new Regex(
            @"(?<comment>///[^\n]*|//[^\n]*|/\*[\s\S]*?\*/)" +
            @"|(?<string>@""(?:[^""]|"""")*""|\$?""(?:\\.|[^""\\\n])*"")" +
            @"|(?<chr>'(?:\\.|[^'\\])')" +
            @"|(?<preproc>^[ \t]*#\w+[^\n]*)" +
            @"|(?<number>\b(?:0x[0-9a-fA-F]+|\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)[fFdDmMlLuU]*\b)" +
            @"|(?<attribute>\[(?:[A-Z]\w*)(?:\([^\)\n]*\))?(?:\s*,\s*[A-Z]\w*(?:\([^\)\n]*\))?)*\])" +
            @"|(?<word>\b[A-Za-z_]\w*\b)",
            RegexOptions.Compiled | RegexOptions.Multiline
        );

        public static string Highlight(string code)
        {
            if (string.IsNullOrEmpty(code)) return code;

            var sb = new StringBuilder(code.Length + 256);
            int pos = 0;

            foreach (Match m in TokenRegex.Matches(code))
            {
                if (m.Index > pos)
                    AppendEscaped(sb, code, pos, m.Index - pos);

                var color = DetermineColor(m, code);
                AppendToken(sb, m.Value, color);
                pos = m.Index + m.Length;
            }

            if (pos < code.Length)
                AppendEscaped(sb, code, pos, code.Length - pos);

            return sb.ToString();
        }

        static string DetermineColor(Match m, string fullCode)
        {
            if (m.Groups["comment"].Success) return CommentColor;
            if (m.Groups["string"].Success) return StringColor;
            if (m.Groups["chr"].Success) return StringColor;
            if (m.Groups["preproc"].Success) return PreprocColor;
            if (m.Groups["number"].Success) return NumberColor;
            if (m.Groups["attribute"].Success) return AttributeColor;
            if (m.Groups["word"].Success)
            {
                var word = m.Value;
                if (Keywords.Contains(word)) return KeywordColor;
                if (ControlKeywords.Contains(word)) return ControlColor;
                if (Types.Contains(word)) return TypeColor;

                // Method detection: followed by '(' (skipping whitespace)
                int endIdx = m.Index + m.Length;
                while (endIdx < fullCode.Length && (fullCode[endIdx] == ' ' || fullCode[endIdx] == '\t'))
                    endIdx++;
                if (endIdx < fullCode.Length && fullCode[endIdx] == '(')
                    return MethodColor;
            }
            return null;
        }

        static void AppendToken(StringBuilder sb, string token, string color)
        {
            if (color == null)
            {
                AppendEscaped(sb, token, 0, token.Length);
                return;
            }
            sb.Append("<color=").Append(color).Append('>');
            AppendEscaped(sb, token, 0, token.Length);
            sb.Append("</color>");
        }

        // Unity rich text uses '<' as tag opener.
        // We escape '<' so literal '<' in code (e.g. generics, comparisons) renders correctly.
        static void AppendEscaped(StringBuilder sb, string s, int start, int len)
        {
            int end = start + len;
            for (int i = start; i < end; i++)
            {
                char c = s[i];
                if (c == '<') sb.Append("<noparse><</noparse>");
                else sb.Append(c);
            }
        }

        public static bool IsCSharpLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang)) return false;
            switch (lang.ToLowerInvariant())
            {
                case "csharp":
                case "cs":
                case "c#":
                    return true;
                default:
                    return false;
            }
        }
    }
}
