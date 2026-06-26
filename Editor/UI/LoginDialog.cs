using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.Core;

namespace ClaudeCode.Editor.UI
{
    public class LoginDialog
    {
        readonly EditorWindow _ownerWindow;
        readonly Action _onComplete;
        readonly AuthManager _authManager;
        VisualElement _overlay;

        Process _bashProcess;
        Label _statusLabel;
        VisualElement _spinner;

        bool _completed;

        // ERR_UNSAFE_PORT 우회: 붙여넣은 콜백 URL을 백그라운드 스레드에서 전달한 결과.
        volatile bool _deliverDone;
        volatile bool _deliverOk;
        string _deliverMsg;

        public LoginDialog(EditorWindow owner, AuthManager authManager, Action onComplete)
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
            box.style.width = 480;
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

            var title = new Label("Sign in to Claude Code");
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 10;
            box.Add(title);

            var instructions = new Label(
                "A terminal has been opened running Claude Code.\n\n" +
                "In the terminal:\n" +
                "  1. Type /login and press Enter.\n" +
                "  2. Your browser will open. Sign in to Claude.\n" +
                "  3. Copy the authorization code from the browser.\n" +
                "  4. Paste it into the terminal (right-click or Ctrl+V).\n" +
                "  5. Press Enter.\n\n" +
                "브라우저가 'ERR_UNSAFE_PORT' 또는 연결 불가를 표시하면, 아래 '브라우저 연결 실패 시' 안내를 따르세요.\n" +
                "This window will detect login completion automatically.");
            instructions.style.fontSize = 11;
            instructions.style.color = new Color(0.8f, 0.8f, 0.8f);
            instructions.style.whiteSpace = WhiteSpace.Normal;
            instructions.style.marginBottom = 14;
            box.Add(instructions);

            BuildStatusRow(box);
            BuildUnsafePortBypass(box);
            BuildApiKeyAlternative(box);
            BuildFooter(box);

            _overlay.Add(box);
            StartLogin();
            return _overlay;
        }

        void BuildStatusRow(VisualElement box)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.paddingTop = 10;
            row.style.paddingBottom = 10;
            row.style.borderTopLeftRadius = 6;
            row.style.borderTopRightRadius = 6;
            row.style.borderBottomLeftRadius = 6;
            row.style.borderBottomRightRadius = 6;
            row.style.marginBottom = 14;

            _spinner = new VisualElement();
            _spinner.style.width = 10;
            _spinner.style.height = 10;
            _spinner.style.borderTopLeftRadius = 5;
            _spinner.style.borderTopRightRadius = 5;
            _spinner.style.borderBottomLeftRadius = 5;
            _spinner.style.borderBottomRightRadius = 5;
            _spinner.style.backgroundColor = new Color(0.95f, 0.7f, 0.2f);
            _spinner.style.marginRight = 10;
            row.Add(_spinner);

            _statusLabel = new Label("Starting terminal...");
            _statusLabel.style.fontSize = 11;
            _statusLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
            _statusLabel.style.flexGrow = 1;
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            row.Add(_statusLabel);

            box.Add(row);
        }

        void BuildUnsafePortBypass(VisualElement box)
        {
            var header = new Label("브라우저 연결 실패(ERR_UNSAFE_PORT) 시:");
            header.style.fontSize = 11;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.9f, 0.8f, 0.5f);
            header.style.marginBottom = 4;
            box.Add(header);

            var desc = new Label(
                "승인 후 브라우저 주소창의 URL(http://localhost:.../callback?code=...)을 " +
                "복사해서 아래에 붙여넣고 '로그인 완료'를 누르세요.");
            desc.style.fontSize = 10;
            desc.style.color = new Color(0.75f, 0.75f, 0.78f);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginBottom = 6;
            box.Add(desc);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 14;

            var field = new TextField();
            field.style.flexGrow = 1;
            field.style.marginRight = 6;
            var inputEl = field.Q<VisualElement>("unity-text-input");
            if (inputEl != null) inputEl.style.minWidth = 0;
            row.Add(field);

            var btn = new Button(() => DeliverPastedCallback(field.value)) { text = "로그인 완료" };
            btn.style.width = 90;
            btn.style.flexShrink = 0;
            row.Add(btn);

            box.Add(row);
        }

        void DeliverPastedCallback(string url)
        {
            url = (url ?? "").Trim();
            if (string.IsNullOrEmpty(url))
            {
                _statusLabel.text = "주소창 URL을 붙여넣어 주세요.";
                return;
            }
            if (!url.StartsWith("http://localhost") && !url.StartsWith("http://127.0.0.1"))
            {
                _statusLabel.text = "localhost 콜백 URL만 가능합니다 (예: http://localhost:6667/callback?code=...).";
                return;
            }

            _deliverDone = false;
            _deliverOk = false;
            _statusLabel.text = "콜백을 CLI로 전달하는 중...";

            var target = url;
            var thread = new Thread(() =>
            {
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(target);
                    req.Method = "GET";
                    req.Timeout = 10000;
                    using var resp = (HttpWebResponse)req.GetResponse();
                    int code = (int)resp.StatusCode;
                    _deliverOk = code >= 200 && code < 400;
                    _deliverMsg = $"HTTP {code}";
                }
                catch (Exception e)
                {
                    _deliverOk = false;
                    _deliverMsg = e.Message;
                }
                finally
                {
                    _deliverDone = true;
                }
            }) { IsBackground = true };
            thread.Start();

            // 백그라운드 결과를 메인 스레드에서 안전하게 반영. 성공 시 로그인 완료는
            // 기존 PollAuthStatus가 감지한다.
            IVisualElementScheduledItem item = null;
            item = _ownerWindow.rootVisualElement.schedule.Execute(() =>
            {
                if (!_deliverDone) return;
                if (_deliverOk)
                    _statusLabel.text = "콜백 전달 완료. 로그인 확인 중...";
                else
                {
                    _statusLabel.text = $"콜백 전달 실패: {_deliverMsg}";
                    SetSpinnerError();
                }
                item.Pause();
            }).Every(300);
        }

        void BuildApiKeyAlternative(VisualElement box)
        {
            var apiBtn = new Button(SwitchToApiKey) { text = "Use API Key Instead" };
            apiBtn.style.height = 24;
            apiBtn.style.backgroundColor = new Color(0.28f, 0.28f, 0.32f);
            apiBtn.style.color = new Color(0.85f, 0.85f, 0.85f);
            apiBtn.style.borderTopLeftRadius = 6;
            apiBtn.style.borderTopRightRadius = 6;
            apiBtn.style.borderBottomLeftRadius = 6;
            apiBtn.style.borderBottomRightRadius = 6;
            apiBtn.style.marginBottom = 12;
            box.Add(apiBtn);
        }

        void BuildFooter(VisualElement box)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.FlexEnd;

            var cancel = new Button(Cancel) { text = "Cancel" };
            cancel.style.width = 80;
            cancel.style.height = 26;
            row.Add(cancel);

            box.Add(row);
        }

        void StartLogin()
        {
            var cliPath = CliLocator.FindClaudeCli();
            if (string.IsNullOrEmpty(cliPath))
            {
                _statusLabel.text = "Claude Code CLI not found.";
                SetSpinnerError();
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k \"\"{cliPath}\"\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                _bashProcess = Process.Start(psi);
                if (_bashProcess == null)
                {
                    _statusLabel.text = "Failed to start terminal.";
                    SetSpinnerError();
                    return;
                }

                _statusLabel.text = "Terminal opened. Type /login in the terminal to begin. This window will detect completion.";
                PollAuthStatus();
            }
            catch (Exception e)
            {
                _statusLabel.text = $"Error: {e.Message}";
                SetSpinnerError();
            }
        }

        void PollAuthStatus()
        {
            _ownerWindow.rootVisualElement.schedule.Execute(() =>
            {
                if (_completed) return;
                var info = _authManager.GetAccountInfo(forceRefresh: true);
                if (info != null && info.loggedIn)
                    CompleteSuccess();
            }).Every(2000).Until(() => _completed);
        }

        void CompleteSuccess()
        {
            _completed = true;
            _statusLabel.text = "Signed in successfully!";
            _spinner.style.backgroundColor = new Color(0.3f, 0.75f, 0.35f);
            _onComplete?.Invoke();
            _ownerWindow.rootVisualElement.schedule.Execute(() => _overlay.RemoveFromHierarchy()).StartingIn(800);
        }

        void SetSpinnerError()
        {
            _spinner.style.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        }

        void SwitchToApiKey()
        {
            CleanupProcesses();
            _overlay.RemoveFromHierarchy();
            var apiDialog = new ApiKeyDialog(_ownerWindow, _authManager, _onComplete);
            _ownerWindow.rootVisualElement.Add(apiDialog.Build());
            _ownerWindow.Repaint();
        }

        void Cancel()
        {
            _completed = true;
            CleanupProcesses();
            _overlay.RemoveFromHierarchy();
        }

        void CleanupProcesses()
        {
            try
            {
                if (_bashProcess != null && !_bashProcess.HasExited)
                    _bashProcess.Kill();
            }
            catch { }
        }
    }
}
