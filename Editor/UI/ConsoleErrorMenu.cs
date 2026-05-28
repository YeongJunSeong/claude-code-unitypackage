using UnityEditor;
using UnityEngine;
using ClaudeCode.Editor.Context;

namespace ClaudeCode.Editor.UI
{
    public static class ConsoleErrorMenu
    {
        [MenuItem("Tools/Claude Code/Fix Latest Console Error", false, 10)]
        static void FixLatestError()
        {
            var latest = ConsoleLogProvider.LatestError;
            if (latest == null)
            {
                EditorUtility.DisplayDialog("Claude Code", "최근 캡쳐된 에러가 없습니다.", "OK");
                return;
            }

            var window = ChatWindow.ShowAndFocus();
            window.StartFixSessionForError(latest);
        }

        [MenuItem("Tools/Claude Code/Fix Latest Console Error", true)]
        static bool ValidateFixLatestError() => ConsoleLogProvider.ErrorCount > 0;

        [MenuItem("Tools/Claude Code/Clear Captured Errors", false, 11)]
        static void ClearErrors()
        {
            ConsoleLogProvider.ClearErrors();
        }

        [MenuItem("Tools/Claude Code/Clear Captured Errors", true)]
        static bool ValidateClearErrors() => ConsoleLogProvider.ErrorCount > 0;
    }
}
