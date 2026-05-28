using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.Core;
using ClaudeCode.Editor.Approval;

namespace ClaudeCode.Editor.UI
{
    public class ClaudeCodeSettingsProvider : UnityEditor.SettingsProvider
    {
        readonly AuthManager _authManager = new AuthManager();

        ClaudeCodeSettingsProvider()
            : base("Project/Claude Code", SettingsScope.Project)
        {
            keywords = new HashSet<string> { "claude", "ai", "assistant", "api", "mcp" };
        }

        [SettingsProvider]
        public static UnityEditor.SettingsProvider CreateSettingsProvider()
        {
            return new ClaudeCodeSettingsProvider();
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Claude Code Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawCliSection();
            EditorGUILayout.Space(8);
            DrawModelSection();
            EditorGUILayout.Space(8);
            DrawAuthSection();
            EditorGUILayout.Space(8);
            DrawApprovalSection();
        }

        void DrawModelSection()
        {
            EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);
            var names = ModelManager.GetDisplayNames();
            var current = Mathf.Max(0, ModelManager.GetCurrentIndex());
            var selected = EditorGUILayout.Popup("Claude Model", current, names.ToArray());
            if (selected != current)
                ModelManager.SetByDisplayName(names[selected]);
        }

        void DrawCliSection()
        {
            EditorGUILayout.LabelField("CLI", EditorStyles.boldLabel);

            var cliPath = CliLocator.FindClaudeCli();
            if (!string.IsNullOrEmpty(cliPath))
            {
                EditorGUILayout.HelpBox($"CLI found: {cliPath}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Claude Code CLI not found.\nInstall it or set the CLAUDE_CLI_PATH environment variable.",
                    MessageType.Warning);
            }
        }

        void DrawAuthSection()
        {
            EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);

            var method = (AuthMethod)EditorGUILayout.EnumPopup("Method", _authManager.Method);
            if (method != _authManager.Method)
                _authManager.Method = method;

            if (method == AuthMethod.ApiKey)
            {
                EditorGUILayout.HelpBox(
                    "Get your API key from console.anthropic.com → Settings → API Keys",
                    MessageType.Info);

                if (GUILayout.Button("Open Anthropic Console", GUILayout.Width(200)))
                    Application.OpenURL("https://console.anthropic.com/settings/keys");

                EditorGUILayout.Space(4);

                var key = EditorGUILayout.PasswordField("API Key", _authManager.ApiKey);
                if (key != _authManager.ApiKey)
                {
                    _authManager.ApiKey = key;
                    _authManager.InvalidateCache();
                }

                var authenticated = _authManager.IsAuthenticated();
                EditorGUILayout.LabelField("Status", authenticated ? "Authenticated" : "Not authenticated");
                return;
            }

            var info = _authManager.GetAccountInfo();

            if (info != null && info.loggedIn)
            {
                EditorGUILayout.LabelField("Email", info.email ?? "-");
                EditorGUILayout.LabelField("Plan", info.subscriptionType ?? "-");
                EditorGUILayout.LabelField("Org", info.orgName ?? "-");
                EditorGUILayout.LabelField("Auth Method", info.authMethod ?? "-");
                EditorGUILayout.LabelField("Provider", info.apiProvider ?? "-");

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh", GUILayout.Width(100)))
                {
                    _authManager.InvalidateCache();
                    _authManager.GetAccountInfo(forceRefresh: true);
                }
                if (GUILayout.Button("Logout", GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("Logout", $"Log out from {info.email}?", "Logout", "Cancel"))
                        _authManager.Logout();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Not signed in.", MessageType.Warning);
                if (GUILayout.Button("Open Claude Code Window to Sign In", GUILayout.Width(280)))
                    ChatWindow.ShowWindow();
            }
        }

        void DrawApprovalSection()
        {
            EditorGUILayout.LabelField("Permission Mode", EditorStyles.boldLabel);

            var current = PermissionModeManager.Current;
            var options = new[]
            {
                PermissionModeManager.DisplayName(PermissionMode.PermissionRequest),
                PermissionModeManager.DisplayName(PermissionMode.AcceptEdits),
                PermissionModeManager.DisplayName(PermissionMode.PlanMode),
            };
            var selected = EditorGUILayout.Popup("Mode", (int)current, options);
            if (selected != (int)current)
                PermissionModeManager.Current = (PermissionMode)selected;

            EditorGUILayout.HelpBox(PermissionModeManager.Description(PermissionModeManager.Current), MessageType.Info);
        }
    }
}
