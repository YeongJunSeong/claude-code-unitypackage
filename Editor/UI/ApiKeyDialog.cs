using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.Core;

namespace ClaudeCode.Editor.UI
{
    public class ApiKeyDialog
    {
        const string ConsoleUrl = "https://console.anthropic.com/settings/keys";

        readonly EditorWindow _ownerWindow;
        readonly AuthManager _authManager;
        readonly Action _onComplete;
        VisualElement _overlay;
        TextField _keyField;
        Label _statusLabel;

        public ApiKeyDialog(EditorWindow owner, AuthManager authManager, Action onComplete)
        {
            _ownerWindow = owner;
            _authManager = authManager;
            _onComplete = onComplete;
        }

        public VisualElement Build()
        {
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;

            var box = new VisualElement();
            box.style.width = 460;
            box.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            box.style.borderTopLeftRadius = 10;
            box.style.borderTopRightRadius = 10;
            box.style.borderBottomLeftRadius = 10;
            box.style.borderBottomRightRadius = 10;
            box.style.paddingLeft = 22;
            box.style.paddingRight = 22;
            box.style.paddingTop = 20;
            box.style.paddingBottom = 18;
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderTopColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderBottomColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderLeftColor = new Color(0.35f, 0.35f, 0.4f);
            box.style.borderRightColor = new Color(0.35f, 0.35f, 0.4f);

            var title = new Label("Sign in with API Key");
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 12;
            box.Add(title);

            var step1 = new Label("1. Get an API key from the Anthropic Console:");
            step1.style.fontSize = 11;
            step1.style.color = new Color(0.7f, 0.7f, 0.7f);
            step1.style.marginBottom = 4;
            box.Add(step1);

            var consoleLink = new Button(() => Application.OpenURL(ConsoleUrl)) { text = "Open Anthropic Console" };
            consoleLink.style.height = 26;
            consoleLink.style.marginBottom = 14;
            consoleLink.style.backgroundColor = new Color(0.25f, 0.45f, 0.75f);
            consoleLink.style.color = Color.white;
            consoleLink.style.borderTopLeftRadius = 6;
            consoleLink.style.borderTopRightRadius = 6;
            consoleLink.style.borderBottomLeftRadius = 6;
            consoleLink.style.borderBottomRightRadius = 6;
            box.Add(consoleLink);

            var step2 = new Label("2. Paste your API key (starts with 'sk-ant-'):");
            step2.style.fontSize = 11;
            step2.style.color = new Color(0.7f, 0.7f, 0.7f);
            step2.style.marginBottom = 4;
            box.Add(step2);

            _keyField = new TextField { isPasswordField = true };
            _keyField.style.height = 28;
            _keyField.style.marginBottom = 8;
            _keyField.style.overflow = Overflow.Hidden;
            var keyInput = _keyField.Q<VisualElement>("unity-text-input");
            if (keyInput != null)
            {
                keyInput.style.minWidth = 0;
                keyInput.style.flexShrink = 1;
            }
            box.Add(_keyField);

            _statusLabel = new Label("");
            _statusLabel.style.fontSize = 10;
            _statusLabel.style.color = new Color(0.65f, 0.65f, 0.65f);
            _statusLabel.style.marginBottom = 14;
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            box.Add(_statusLabel);

            BuildFooter(box);

            _overlay.Add(box);
            return _overlay;
        }

        void BuildFooter(VisualElement box)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;

            var oauthBtn = new Button(SwitchToOAuth) { text = "Use Browser Sign-in Instead" };
            oauthBtn.style.height = 26;
            oauthBtn.style.paddingLeft = 8;
            oauthBtn.style.paddingRight = 8;
            oauthBtn.style.backgroundColor = new Color(0.28f, 0.28f, 0.32f);
            oauthBtn.style.color = new Color(0.85f, 0.85f, 0.85f);
            row.Add(oauthBtn);

            var rightGroup = new VisualElement();
            rightGroup.style.flexDirection = FlexDirection.Row;

            var cancel = new Button(Cancel) { text = "Cancel" };
            cancel.style.width = 80;
            cancel.style.height = 26;
            cancel.style.marginRight = 6;
            rightGroup.Add(cancel);

            var save = new Button(Save) { text = "Save" };
            save.style.width = 80;
            save.style.height = 26;
            save.style.backgroundColor = new Color(0.2f, 0.55f, 0.3f);
            save.style.color = Color.white;
            save.style.borderTopLeftRadius = 6;
            save.style.borderTopRightRadius = 6;
            save.style.borderBottomLeftRadius = 6;
            save.style.borderBottomRightRadius = 6;
            rightGroup.Add(save);

            row.Add(rightGroup);
            box.Add(row);
        }

        void Save()
        {
            var key = _keyField.value?.Trim();
            if (string.IsNullOrEmpty(key))
            {
                _statusLabel.text = "Please paste your API key.";
                _statusLabel.style.color = new Color(0.9f, 0.4f, 0.4f);
                return;
            }

            if (!key.StartsWith("sk-ant-"))
            {
                _statusLabel.text = "Warning: Anthropic API keys usually start with 'sk-ant-'. Saving anyway.";
                _statusLabel.style.color = new Color(0.9f, 0.7f, 0.3f);
            }

            _authManager.Method = AuthMethod.ApiKey;
            _authManager.ApiKey = key;
            _authManager.InvalidateCache();

            _overlay.RemoveFromHierarchy();
            _onComplete?.Invoke();
        }

        void SwitchToOAuth()
        {
            _overlay.RemoveFromHierarchy();
            var oauthDialog = new LoginDialog(_ownerWindow, _authManager, _onComplete);
            _ownerWindow.rootVisualElement.Add(oauthDialog.Build());
            _ownerWindow.Repaint();
        }

        void Cancel()
        {
            _overlay.RemoveFromHierarchy();
        }
    }
}
