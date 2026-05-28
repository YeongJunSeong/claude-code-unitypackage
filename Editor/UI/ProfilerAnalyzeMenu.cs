using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ClaudeCode.Editor.Context;

namespace ClaudeCode.Editor.UI
{
    public static class ProfilerAnalyzeMenu
    {
        // %#p = Ctrl+Shift+P (Cmd+Shift+P on macOS)
        [MenuItem("Tools/Claude Code/Analyze Profiler Data %#p", false, 20)]
        static void AnalyzeProfiler()
        {
            int first = ProfilerDriver.firstFrameIndex;
            int last = ProfilerDriver.lastFrameIndex;
            if (last < 0 || last < first)
            {
                EditorUtility.DisplayDialog(
                    "Claude Code",
                    "Profiler에 녹화된 프레임이 없습니다.\n\nPlay Mode를 켜고 Profiler 창에서 데이터가 수집된 뒤 다시 시도해주세요.",
                    "OK");
                return;
            }

            var result = ProfilerSnapshot.CaptureAll();
            var text = ProfilerSnapshot.FormatAsText(result);

            var window = ChatWindow.ShowAndFocus();
            window.StartProfilerAnalysisSession(text);
        }

        [MenuItem("Tools/Claude Code/Analyze Profiler Data %#p", true)]
        static bool ValidateAnalyzeProfiler() => true;
    }
}
