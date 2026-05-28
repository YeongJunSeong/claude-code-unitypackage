using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.Core;

namespace ClaudeCode.Editor.UI
{
    public static class AccountPopup
    {
        public static VisualElement Build(AuthManager authManager, Action onLogout, Action onRefresh, Action onLoginRequested = null)
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.4f);
            overlay.style.alignItems = Align.FlexEnd;
            overlay.style.justifyContent = Justify.FlexStart;
            overlay.style.paddingTop = 30;
            overlay.style.paddingRight = 8;

            overlay.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.target == overlay)
                    overlay.RemoveFromHierarchy();
            });

            var box = new VisualElement();
            box.style.width = 260;
            box.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            box.style.borderTopLeftRadius = 8;
            box.style.borderTopRightRadius = 8;
            box.style.borderBottomLeftRadius = 8;
            box.style.borderBottomRightRadius = 8;
            box.style.paddingLeft = 14;
            box.style.paddingRight = 14;
            box.style.paddingTop = 12;
            box.style.paddingBottom = 12;
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderTopColor = new Color(0.32f, 0.32f, 0.36f);
            box.style.borderBottomColor = new Color(0.32f, 0.32f, 0.36f);
            box.style.borderLeftColor = new Color(0.32f, 0.32f, 0.36f);
            box.style.borderRightColor = new Color(0.32f, 0.32f, 0.36f);

            var info = authManager.GetAccountInfo();

            if (info == null || !info.loggedIn)
            {
                BuildLoggedOutView(box, overlay, onRefresh, onLoginRequested);
            }
            else
            {
                BuildLoggedInView(box, info, authManager, overlay, onLogout, onRefresh);
            }

            overlay.Add(box);
            return overlay;
        }

        static void BuildLoggedInView(VisualElement box, AccountInfo info, AuthManager authManager, VisualElement overlay, Action onLogout, Action onRefresh)
        {
            var title = new Label("Account");
            title.style.fontSize = 11;
            title.style.color = new Color(0.6f, 0.6f, 0.6f);
            title.style.marginBottom = 6;
            box.Add(title);

            var email = new Label(info.email ?? "(no email)");
            email.style.fontSize = 13;
            email.style.unityFontStyleAndWeight = FontStyle.Bold;
            email.style.color = Color.white;
            email.style.marginBottom = 10;
            box.Add(email);

            AddRow(box, "Plan", info.subscriptionType ?? "-");
            AddRow(box, "Org", info.orgName ?? "-");
            AddRow(box, "Auth", info.authMethod ?? "-");
            AddRow(box, "Provider", info.apiProvider ?? "-");

            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.32f);
            separator.style.marginTop = 10;
            separator.style.marginBottom = 10;
            box.Add(separator);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.SpaceBetween;

            var refreshBtn = new Button(() =>
            {
                authManager.InvalidateCache();
                overlay.RemoveFromHierarchy();
                onRefresh?.Invoke();
            }) { text = "Refresh" };
            refreshBtn.style.width = 100;
            refreshBtn.style.height = 26;
            buttonRow.Add(refreshBtn);

            var logoutBtn = new Button(() =>
            {
                if (EditorUtility.DisplayDialog("Logout", $"Log out from {info.email}?", "Logout", "Cancel"))
                {
                    authManager.Logout();
                    overlay.RemoveFromHierarchy();
                    onLogout?.Invoke();
                }
            }) { text = "Logout" };
            logoutBtn.style.width = 100;
            logoutBtn.style.height = 26;
            logoutBtn.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);
            logoutBtn.style.color = Color.white;
            buttonRow.Add(logoutBtn);

            box.Add(buttonRow);
        }

        static void BuildLoggedOutView(VisualElement box, VisualElement overlay, Action onRefresh, Action onLoginRequested)
        {
            var title = new Label("Not signed in");
            title.style.fontSize = 13;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 8;
            box.Add(title);

            var desc = new Label("Sign in to Claude Code to start chatting.");
            desc.style.fontSize = 11;
            desc.style.color = new Color(0.7f, 0.7f, 0.7f);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginBottom = 12;
            box.Add(desc);

            var loginBtn = new Button(() =>
            {
                overlay.RemoveFromHierarchy();
                onLoginRequested?.Invoke();
            }) { text = "Sign in" };
            loginBtn.style.height = 28;
            loginBtn.style.backgroundColor = new Color(0.25f, 0.45f, 0.75f);
            loginBtn.style.color = Color.white;
            box.Add(loginBtn);
        }

        static void AddRow(VisualElement box, string label, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 4;

            var lbl = new Label(label);
            lbl.style.fontSize = 11;
            lbl.style.color = new Color(0.6f, 0.6f, 0.6f);
            row.Add(lbl);

            var val = new Label(value);
            val.style.fontSize = 11;
            val.style.color = new Color(0.9f, 0.9f, 0.9f);
            row.Add(val);

            box.Add(row);
        }
    }
}
