using UnityEditor;

namespace ClaudeCode.Editor.Approval
{
    public enum PermissionMode
    {
        PermissionRequest = 0,   // 권한 요청 — 모든 도구 사용 시 사용자 승인 필요
        AcceptEdits = 1,         // 편집 수락 — 파일 편집은 자동 승인, 그 외는 요청
        PlanMode = 2             // 계획 모드 — 읽기 전용, 수정 불가
    }

    [InitializeOnLoad]
    public static class PermissionModeManager
    {
        const string PrefKey = "ClaudeCode_PermissionMode";
        static volatile int _cached;

        static PermissionModeManager()
        {
            _cached = EditorPrefs.GetInt(PrefKey, (int)PermissionMode.PermissionRequest);
        }

        public static PermissionMode Current
        {
            get => (PermissionMode)_cached;
            set
            {
                _cached = (int)value;
                EditorPrefs.SetInt(PrefKey, _cached);
            }
        }

        public static string DisplayName(PermissionMode mode) => mode switch
        {
            PermissionMode.PermissionRequest => "Permission Request",
            PermissionMode.AcceptEdits => "Accept Edits",
            PermissionMode.PlanMode => "Plan Mode",
            _ => mode.ToString()
        };

        public static string Description(PermissionMode mode) => mode switch
        {
            PermissionMode.PermissionRequest => "All tool uses require user approval (safest).",
            PermissionMode.AcceptEdits => "File edits are auto-approved; other tools still require approval.",
            PermissionMode.PlanMode => "Read-only. Claude analyzes and plans without modifying files or the scene.",
            _ => ""
        };

        public static string Icon(PermissionMode mode) => mode switch
        {
            PermissionMode.PermissionRequest => "",
            PermissionMode.AcceptEdits => "",
            PermissionMode.PlanMode => "",
            _ => ""
        };

        public static string CliFlag(PermissionMode mode) => mode switch
        {
            PermissionMode.PermissionRequest => "default",
            PermissionMode.AcceptEdits => "acceptEdits",
            PermissionMode.PlanMode => "plan",
            _ => "default"
        };
    }
}
