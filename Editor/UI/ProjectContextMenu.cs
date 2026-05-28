using System.IO;
using UnityEditor;
using UnityEngine;

namespace ClaudeCode.Editor.UI
{
    public static class ProjectContextMenu
    {
        const string Root = "Assets/Ask Claude/";

        // ---- C# Script actions ----

        [MenuItem(Root + "Explain this code", false, 0)]
        static void ExplainScript() => RunForScript("이 스크립트의 동작과 의도를 한국어로 자세히 설명해줘.");
        [MenuItem(Root + "Explain this code", true)]
        static bool ValExplainScript() => IsScript();

        [MenuItem(Root + "Refactor this code", false, 1)]
        static void RefactorScript() => RunForScript("이 스크립트를 가독성/구조/성능 측면에서 리팩토링 해줘. 변경 의도를 함께 설명.");
        [MenuItem(Root + "Refactor this code", true)]
        static bool ValRefactorScript() => IsScript();

        [MenuItem(Root + "Generate unit tests", false, 2)]
        static void GenTests() => RunForScript("이 스크립트에 대한 Unity Test Framework 기반 단위 테스트를 작성해줘.");
        [MenuItem(Root + "Generate unit tests", true)]
        static bool ValGenTests() => IsScript();

        [MenuItem(Root + "Review for bugs", false, 3)]
        static void ReviewBugs() => RunForScript("이 스크립트에서 잠재적인 버그/논리 오류/예외 케이스를 찾아줘.");
        [MenuItem(Root + "Review for bugs", true)]
        static bool ValReviewBugs() => IsScript();

        [MenuItem(Root + "Add XML comments", false, 4)]
        static void AddComments() => RunForScript("이 스크립트의 public API에 XML doc comments를 추가해줘. 기존 코드 동작은 유지.");
        [MenuItem(Root + "Add XML comments", true)]
        static bool ValAddComments() => IsScript();

        // ---- Prefab actions ----

        [MenuItem(Root + "Analyze prefab structure", false, 0)]
        static void AnalyzePrefab() => RunGeneric("이 프리팹의 구조(하이어라키, 컴포넌트, 의존성)를 분석하고 설명해줘.");
        [MenuItem(Root + "Analyze prefab structure", true)]
        static bool ValAnalyzePrefab() => IsPrefab();

        [MenuItem(Root + "Optimize prefab", false, 1)]
        static void OptimizePrefab() => RunGeneric("이 프리팹의 성능/메모리 측면에서 최적화 방안을 제안해줘.");
        [MenuItem(Root + "Optimize prefab", true)]
        static bool ValOptimizePrefab() => IsPrefab();

        // ---- Shader / Material actions ----

        [MenuItem(Root + "Explain this shader", false, 0)]
        static void ExplainShader() => RunGeneric("이 셰이더/머터리얼이 어떤 효과를 내는지 설명해줘.");
        [MenuItem(Root + "Explain this shader", true)]
        static bool ValExplainShader() => IsShaderOrMaterial();

        // ---- Generic ----

        [MenuItem(Root + "Explain this file", false, 10)]
        static void ExplainFile() => RunGeneric("이 파일의 역할과 내용을 설명해줘.");
        [MenuItem(Root + "Explain this file", true)]
        static bool ValExplainFile() => GetAssetPath() != null && !IsScript() && !IsPrefab() && !IsShaderOrMaterial();

        // ---- Add to chat (always available) ----

        [MenuItem(Root + "Add to chat", false, 30)]
        static void AddToChat()
        {
            var path = GetAssetPath();
            if (string.IsNullOrEmpty(path)) return;
            var window = ChatWindow.ShowAndFocus();
            window.AttachAssetOnly(path);
        }
        [MenuItem(Root + "Add to chat", true)]
        static bool ValAddToChat() => GetAssetPath() != null;

        // ---- Helpers ----

        static void RunForScript(string prompt) => RunGeneric(prompt);

        static void RunGeneric(string prompt)
        {
            var path = GetAssetPath();
            if (string.IsNullOrEmpty(path)) return;
            var window = ChatWindow.ShowAndFocus();
            window.StartTaskWithAsset(path, prompt);
        }

        static string GetAssetPath()
        {
            if (Selection.activeObject == null) return null;
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return string.IsNullOrEmpty(path) ? null : path;
        }

        static string GetExtension()
        {
            var p = GetAssetPath();
            return p == null ? "" : Path.GetExtension(p).ToLowerInvariant();
        }

        static bool IsScript() => GetExtension() == ".cs";
        static bool IsPrefab() => GetExtension() == ".prefab";
        static bool IsShaderOrMaterial()
        {
            var ext = GetExtension();
            return ext == ".shader" || ext == ".mat" || ext == ".shadergraph";
        }
    }
}
