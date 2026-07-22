using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;
using ClaudeCode.Editor.Core;
using ClaudeCode.Editor.History;
using ClaudeCode.Editor.MCP;

namespace ClaudeCode.Editor.UI
{
    public class ChatWindow : EditorWindow
    {
        SessionManager _session;
        AuthManager _authManager;
        McpServer _mcpServer;

        ScrollView _messageContainer;
        TextField _inputField;
        Button _sendButton;
        Button _stopButton;
        Button _clearButton;
        Button _historyToggleButton;
        Button _accountButton;
        Button _errorBadge;
        Button _permissionButton;
        Button _claudeMdButton;
        VisualElement _profileBadge;
        Label _profileBadgeLabel;
        Button _effortButton;
        Button _updateBadge;
        VisualElement _compileToast;
        VisualElement _statusBar;
        VisualElement _statusDot;
        Label _statusLabel;
        VisualElement _inputArea;
        VisualElement _sidebarContainer;
        VisualElement _mainPane;
        AttachmentBar _attachmentBar;
        SlashCommandPopup _slashPopup;
        ContextTagPopup _tagPopup;
        int _tagStartIndex = -1;
        bool _reloadLocked;
        bool _sidebarVisible = false;

        VisualElement _streamingBubble;
        Label _streamingLabel;
        Label _typingIndicator;
        IVisualElementScheduledItem _typingAnimator;
        int _typingDotsFrame;
        bool _firstDeltaReceived;

        DateTime _streamStartTime;
        IVisualElementScheduledItem _streamTimer;
        string _currentToolName;
        VisualElement _streamingRow;
        Label _streamingTimerLabel;
        UsageIndicator _usageIndicator;

        [MenuItem("Window/Claude Code %#k")]
        public static void ShowWindow()
        {
            var window = GetWindow<ChatWindow>("Claude Code");
            window.minSize = new Vector2(380, 280);
        }

        public static ChatWindow ShowAndFocus()
        {
            var window = GetWindow<ChatWindow>("Claude Code");
            window.minSize = new Vector2(380, 280);
            window.Focus();
            return window;
        }

        public void StartTaskWithAsset(string assetPath, string prompt)
        {
            _attachmentBar.Add(assetPath);
            if (!string.IsNullOrEmpty(prompt))
            {
                SetInputText(prompt);
                Repaint();
                rootVisualElement.schedule.Execute(OnSendClicked).StartingIn(50);
            }
            else
            {
                _inputField.Focus();
            }
        }

        public void AttachAssetOnly(string assetPath)
        {
            _attachmentBar.Add(assetPath);
            Repaint();
            _inputField.Focus();
        }

        public void StartTaskWithGameObject(GameObject go, string prompt)
        {
            if (go == null) return;
            var snippet = BuildGameObjectSnippet(go);
            var combined = string.IsNullOrEmpty(prompt) ? snippet : $"{prompt}\n\n{snippet}";
            SetInputText(combined);
            Repaint();
            if (!string.IsNullOrEmpty(prompt))
                rootVisualElement.schedule.Execute(OnSendClicked).StartingIn(50);
            else
                _inputField.Focus();
        }

        public void AttachGameObjectOnly(GameObject go)
        {
            if (go == null) return;
            var snippet = BuildGameObjectSnippet(go);
            var current = _inputField.value ?? "";
            if (current.Length > 0 && !current.EndsWith("\n")) current += "\n";
            SetInputText(current + snippet);
            Repaint();
            _inputField.Focus();
        }

        static string BuildGameObjectSnippet(GameObject go)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Attached: GameObject]");
            sb.AppendLine($"- {GetGameObjectPath(go)} (active={go.activeSelf}, layer={LayerMask.LayerToName(go.layer)})");
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                sb.AppendLine($"    Component: {comp.GetType().Name}");
            }
            if (go.transform.childCount > 0)
            {
                sb.AppendLine($"  Children ({go.transform.childCount}):");
                for (int i = 0; i < go.transform.childCount && i < 20; i++)
                    sb.AppendLine($"    - {go.transform.GetChild(i).name}");
                if (go.transform.childCount > 20)
                    sb.AppendLine($"    ... +{go.transform.childCount - 20} more");
            }
            return sb.ToString();
        }

        static string GetGameObjectPath(GameObject go)
        {
            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        void OnEnable()
        {
            _authManager = new AuthManager();
            _session = new SessionManager(_authManager);
            _session.OnTextDelta += HandleTextDelta;
            _session.OnMessageComplete += HandleMessageComplete;
            _session.OnDetailedError += HandleDetailedError;
            _session.OnUsageUpdated += HandleUsageUpdated;
            _session.OnToolUse += HandleToolUse;

            _mcpServer = new McpServer();
            _mcpServer.OnPermissionRequested += HandlePermissionRequested;
            _mcpServer.OnFileModificationApproved += HandleFileModificationApproved;
            _mcpServer.Start();

            _slashPopup = new SlashCommandPopup(rootVisualElement, OnSlashCommandSelected);
            _tagPopup = new ContextTagPopup(rootVisualElement, OnTagSelected);

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            Context.ConsoleLogProvider.OnErrorsChanged += OnConsoleErrorsChanged;
            Context.ConsoleLogProvider.OnCompileErrorsDetected += OnCompileErrorsDetected;
            if (_mcpServer.IsRunning)
                _session.McpConfigPath = McpConfigWriter.WriteConfig(_mcpServer.Port);

            BuildUI();
            RestoreSession();
            UpdateConnectionStatus();
            RefreshErrorBadge();
            UpdateUsageIndicator();

            rootVisualElement.schedule.Execute(() =>
            {
                if (_session?.IsProcessing == true)
                {
                    _session.Tick();
                    Repaint();
                }
            }).Every(33);

            rootVisualElement.schedule.Execute(RefreshProfileBadge).Every(250);
            rootVisualElement.schedule.Execute(RefreshUpdateBadge).Every(3000);
            RefreshUpdateBadge();
        }

        void RefreshUpdateBadge()
        {
            if (_updateBadge == null) return;
            if (UpdateChecker.HasUpdate)
            {
                _updateBadge.style.display = DisplayStyle.Flex;
                _updateBadge.text = $"NEW v{UpdateChecker.RemoteVersion}";
                _updateBadge.tooltip =
                    $"새 버전 v{UpdateChecker.RemoteVersion} 사용 가능 (현재 v{UpdateChecker.InstalledVersion}).\n" +
                    "클릭하면 Package Manager가 열립니다 — Update 버튼을 누르거나, 같은 git URL로 다시 Add 하세요.";
            }
            else
            {
                _updateBadge.style.display = DisplayStyle.None;
            }
        }

        void ShowEffortPopup()
        {
            var overlay = EffortPopup.Build(rootVisualElement, _effortButton, RefreshEffortButton);
            rootVisualElement.Add(overlay);
            Repaint();
        }

        void RefreshEffortButton()
        {
            if (_effortButton == null) return;
            _effortButton.text = EffortManager.CurrentDisplayName;
            _effortButton.tooltip = EffortManager.IsDefault
                ? "작업량: CLI 기본값 사용 중. 클릭해서 조절"
                : $"작업량: {EffortManager.CurrentDisplayName} (--effort {EffortManager.Current}). 클릭해서 조절";
        }

        void RefreshProfileBadge()
        {
            if (_profileBadge == null) return;
            if (MCP.Tools.ProfilerSession.IsActive)
            {
                _profileBadge.style.display = DisplayStyle.Flex;
                if (_profileBadgeLabel != null)
                    _profileBadgeLabel.text = $"REC {MCP.Tools.ProfilerSession.ElapsedSeconds:F1}s";
            }
            else
            {
                _profileBadge.style.display = DisplayStyle.None;
            }
        }

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            Context.ConsoleLogProvider.OnErrorsChanged -= OnConsoleErrorsChanged;
            Context.ConsoleLogProvider.OnCompileErrorsDetected -= OnCompileErrorsDetected;

            UnlockReloadIfNeeded();

            _mcpServer?.Dispose();
            McpConfigWriter.DeleteConfig();
            SessionSerializer.instance.SaveState(_session);
            _session?.Dispose();
        }

        void LockReloadIfNeeded()
        {
            if (_reloadLocked) return;
            EditorApplication.LockReloadAssemblies();
            _reloadLocked = true;
        }

        void UnlockReloadIfNeeded()
        {
            if (!_reloadLocked) return;
            try { EditorApplication.UnlockReloadAssemblies(); }
            catch (Exception e) { UnityEngine.Debug.LogWarning($"[ClaudeCode] UnlockReloadAssemblies failed: {e.Message}"); }
            _reloadLocked = false;
        }

        void OnBeforeAssemblyReload()
        {
            // Defensive: if a reload happens despite our lock, release it cleanly.
            if (_reloadLocked)
            {
                UnityEngine.Debug.LogWarning("[ClaudeCode] Domain reload triggered while a response was streaming. Releasing lock.");
                UnlockReloadIfNeeded();
            }
        }

        void OnCompilationStarted(object _)
        {
            if (_reloadLocked)
                ShowToast("응답 처리 중입니다. 컴파일은 응답 완료 후 진행됩니다.");
        }

        void BuildUI()
        {
            var root = rootVisualElement;
            root.Clear();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow = 1;
            root.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            _sidebarContainer = new VisualElement();
            _sidebarContainer.style.display = DisplayStyle.None;
            root.Add(_sidebarContainer);

            _mainPane = new VisualElement();
            _mainPane.style.flexGrow = 1;
            _mainPane.style.flexDirection = FlexDirection.Column;
            root.Add(_mainPane);

            BuildToolbar(_mainPane);
            BuildMessageArea(_mainPane);
            BuildInputArea(_mainPane);
        }

        void RefreshSidebar()
        {
            _sidebarContainer.Clear();
            if (!_sidebarVisible) return;

            var sidebar = HistoryBrowser.Build(
                _session.SessionId,
                LoadSession,
                DeleteSession,
                StartNewSession);
            _sidebarContainer.Add(sidebar);
        }

        void ToggleSidebar()
        {
            _sidebarVisible = !_sidebarVisible;
            _sidebarContainer.style.display = _sidebarVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (_historyToggleButton != null)
                _historyToggleButton.text = _sidebarVisible ? "« Hide" : "History »";
            RefreshSidebar();
        }

        void OnConsoleErrorsChanged()
        {
            RefreshErrorBadge();
        }

        void RefreshErrorBadge()
        {
            if (_errorBadge == null) return;
            int count = Context.ConsoleLogProvider.ErrorCount;
            if (count == 0)
            {
                _errorBadge.style.display = DisplayStyle.None;
            }
            else
            {
                _errorBadge.style.display = DisplayStyle.Flex;
                _errorBadge.text = $"! {count}";
            }
            Repaint();
        }

        void ShowErrorDropdown()
        {
            var dropdown = ErrorListDropdown.Build(
                onFix: err => StartFixSessionForError(err),
                onClearAll: () =>
                {
                    Context.ConsoleLogProvider.ClearErrors();
                    Repaint();
                });
            rootVisualElement.Add(dropdown);
            Repaint();
        }

        void ShowClaudeMdMenu()
        {
            bool exists = Core.ClaudeMdManager.Exists();

            var items = new List<IconDropdownMenu.Item>
            {
                new IconDropdownMenu.Item
                {
                    Icon = IconType.Plus,
                    Label = exists ? "Create  (이미 존재함)" : "Create",
                    Description = "프로젝트 루트에 CLAUDE.md 생성",
                    Disabled = exists,
                    OnClick = RunClaudeMdCreate
                },
                new IconDropdownMenu.Item
                {
                    Icon = IconType.BookOpen,
                    Label = exists ? "Read" : "Read  (CLAUDE.md 없음)",
                    Description = "현재 CLAUDE.md 내용 보기",
                    Disabled = !exists,
                    OnClick = ShowClaudeMdRead
                },
                new IconDropdownMenu.Item
                {
                    Icon = IconType.Edit,
                    Label = exists ? "Update" : "Update  (CLAUDE.md 없음)",
                    Description = "Claude로 분석 후 업데이트",
                    Disabled = !exists,
                    OnClick = RunClaudeMdUpdate
                },
            };

            var anchor = _claudeMdButton ?? rootVisualElement;
            var overlay = IconDropdownMenu.Build(anchor, items, menuWidth: 280);
            rootVisualElement.Add(overlay);
        }

        void ShowPermissionMenu()
        {
            var current = Approval.PermissionModeManager.Current;
            var items = new List<IconDropdownMenu.Item>
            {
                BuildPermissionItem(Approval.PermissionMode.PermissionRequest, current),
                BuildPermissionItem(Approval.PermissionMode.AcceptEdits, current),
                BuildPermissionItem(Approval.PermissionMode.PlanMode, current),
            };
            var anchor = _permissionButton ?? rootVisualElement;
            var overlay = IconDropdownMenu.Build(anchor, items, menuWidth: 280);
            rootVisualElement.Add(overlay);
        }

        IconDropdownMenu.Item BuildPermissionItem(Approval.PermissionMode mode, Approval.PermissionMode current)
        {
            return new IconDropdownMenu.Item
            {
                Icon = PermissionIcon(mode),
                Label = Approval.PermissionModeManager.DisplayName(mode),
                Description = Approval.PermissionModeManager.Description(mode),
                IsCurrent = mode == current,
                OnClick = () =>
                {
                    Approval.PermissionModeManager.Current = mode;
                    RefreshPermissionButton();
                }
            };
        }

        static IconType PermissionIcon(Approval.PermissionMode mode) => mode switch
        {
            Approval.PermissionMode.PermissionRequest => IconType.Lock,
            Approval.PermissionMode.AcceptEdits       => IconType.Check,
            Approval.PermissionMode.PlanMode          => IconType.BookOpen,
            _                                         => IconType.Info
        };

        void RefreshPermissionButton()
        {
            if (_permissionButton == null) return;
            _permissionButton.Clear();
            var current = Approval.PermissionModeManager.Current;

            var icon = VectorIcons.Make(PermissionIcon(current), 12);
            icon.style.marginRight = 6;
            _permissionButton.Add(icon);

            var label = new Label(Approval.PermissionModeManager.DisplayName(current));
            label.style.fontSize = 11;
            label.style.flexGrow = 1;
            label.pickingMode = PickingMode.Ignore;
            _permissionButton.Add(label);

            var chev = VectorIcons.Make(IconType.ChevronDown, 10);
            chev.style.marginLeft = 4;
            _permissionButton.Add(chev);

            _permissionButton.tooltip = Approval.PermissionModeManager.Description(current);
        }

        void ShowClaudeMdRead()
        {
            rootVisualElement.Add(ClaudeMdReadDialog.Build());
            Repaint();
        }

        void RunClaudeMdCreate()
        {
            if (_session == null || _session.IsProcessing) return;
            if (Core.ClaudeMdManager.Exists())
            {
                ShowToast("CLAUDE.md가 이미 존재합니다. Update를 사용하세요.");
                return;
            }

            var dialog = ClaudeMdCreateDialog.Build(targetPath =>
            {
                SendClaudeMdUpdatePrompt(targetPath, isCreate: true);
            });
            rootVisualElement.Add(dialog);
            Repaint();
        }

        void RunClaudeMdUpdate()
        {
            if (_session == null || _session.IsProcessing) return;
            if (!Core.ClaudeMdManager.Exists())
            {
                ShowToast("CLAUDE.md가 없습니다. 먼저 Create를 사용하세요.");
                return;
            }

            SendClaudeMdUpdatePrompt(Core.ClaudeMdManager.GetProjectClaudeMdPath(), isCreate: false);
        }

        void SendClaudeMdUpdatePrompt(string targetPath, bool isCreate)
        {
            var normalized = targetPath.Replace("\\", "/");
            string prompt;

            if (isCreate)
            {
                prompt =
                    $"새로운 CLAUDE.md 파일을 다음 경로에 생성해줘: {normalized}\n" +
                    "\n" +
                    "현재 Unity 프로젝트 상태를 파악한 뒤 마크다운 형식으로 작성:\n" +
                    "- Unity 버전\n" +
                    "- 사용 중인 Render Pipeline\n" +
                    "- Packages/manifest.json 의 주요 패키지 목록\n" +
                    "- Assets/ 폴더 구조 (주요 하위 폴더)\n" +
                    "- 프로젝트 타입 (2D/3D/VR 등) 추정\n" +
                    "- 기본 코드 컨벤션 자리(빈 채로) — 추후 사용자가 채울 수 있도록 섹션 헤더만 둠\n" +
                    "\n" +
                    "한국어로 작성. Write 도구로 위 경로에 파일을 생성해줘.";
            }
            else
            {
                prompt =
                    $"기존 CLAUDE.md 파일을 분석하고 업데이트해줘. 파일 경로: {normalized}\n" +
                    "\n" +
                    "절차:\n" +
                    "1. 먼저 위 경로의 CLAUDE.md를 Read 도구로 읽어줘.\n" +
                    "2. 현재 Unity 프로젝트 상태를 파악해줘:\n" +
                    "   - Unity 버전, Render Pipeline, 주요 패키지, 폴더 구조 등\n" +
                    "3. 기존 내용은 최대한 보존하면서 위 정보를 적절히 추가/갱신.\n" +
                    "   - 기존 컨벤션/가이드 섹션은 건드리지 마.\n" +
                    "   - 누락된 기본 정보 섹션만 추가하거나 갱신.\n" +
                    "4. Edit 도구로 변경 사항을 적용. 한국어 마크다운.\n" +
                    "\n" +
                    "주의: 기존 사용자 작성 내용을 함부로 지우지 마. 추가/갱신만.";
            }

            SetInputText(prompt);
            rootVisualElement.schedule.Execute(OnSendClicked).StartingIn(50);
        }

        public void StartFixSessionForError(Context.ConsoleError err)
        {
            if (err == null) return;

            // Start a fresh session for this error
            _session.ClearHistory();
            _messageContainer.Clear();
            _attachmentBar.Clear();

            // Attach related file if detected
            if (!string.IsNullOrEmpty(err.FilePath))
                _attachmentBar.Add(err.FilePath);

            // Build error info text + analysis prompt
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("다음 Unity 콘솔 에러를 분석해줘. 원인 진단과 가능한 수정 방안을 설명해줘. 수정이 필요하면 어떤 파일/라인을 어떻게 바꿔야 하는지 알려줘.");
            sb.AppendLine();
            sb.AppendLine("[Console Error]");
            sb.AppendLine($"Type: {err.Type}");
            sb.AppendLine($"Message: {err.Message}");
            if (!string.IsNullOrEmpty(err.ShortLocation))
                sb.AppendLine($"Location: {err.FilePath}:{err.LineNumber}");
            if (!string.IsNullOrEmpty(err.StackTrace))
            {
                sb.AppendLine();
                sb.AppendLine("Stack trace:");
                sb.Append(err.StackTrace);
            }

            SetInputText(sb.ToString());
            _inputField.Focus();

            rootVisualElement.schedule.Execute(OnSendClicked).StartingIn(50);
        }

        void OnCompileErrorsDetected(System.Collections.Generic.IReadOnlyList<Context.ConsoleError> errors)
        {
            RefreshErrorBadge();
            ShowCompileErrorToast(errors);
        }

        void ShowCompileErrorToast(System.Collections.Generic.IReadOnlyList<Context.ConsoleError> errors)
        {
            if (errors == null || errors.Count == 0) return;

            _compileToast?.RemoveFromHierarchy();

            var toast = new VisualElement();
            toast.style.position = Position.Absolute;
            toast.style.bottom = 80;
            toast.style.left = Length.Percent(50);
            toast.style.translate = new Translate(Length.Percent(-50), 0);
            toast.style.flexDirection = FlexDirection.Row;
            toast.style.alignItems = Align.Center;
            toast.style.backgroundColor = new Color(0.35f, 0.14f, 0.14f, 0.97f);
            toast.style.paddingLeft = 14;
            toast.style.paddingRight = 8;
            toast.style.paddingTop = 8;
            toast.style.paddingBottom = 8;
            toast.style.borderTopLeftRadius = 8;
            toast.style.borderTopRightRadius = 8;
            toast.style.borderBottomLeftRadius = 8;
            toast.style.borderBottomRightRadius = 8;
            toast.style.borderTopWidth = 1;
            toast.style.borderBottomWidth = 1;
            toast.style.borderLeftWidth = 1;
            toast.style.borderRightWidth = 1;
            var border = new Color(0.6f, 0.3f, 0.3f);
            toast.style.borderTopColor = border;
            toast.style.borderBottomColor = border;
            toast.style.borderLeftColor = border;
            toast.style.borderRightColor = border;

            var msg = new Label($"컴파일 에러 {errors.Count}개 발생");
            msg.style.fontSize = 11;
            msg.style.color = new Color(1f, 0.85f, 0.85f);
            msg.style.marginRight = 10;
            toast.Add(msg);

            var errorList = new System.Collections.Generic.List<Context.ConsoleError>(errors);
            var fixBtn = new Button(() =>
            {
                toast.RemoveFromHierarchy();
                StartFixSessionForCompileErrors(errorList);
            }) { text = "Fix with Claude" };
            fixBtn.style.height = 22;
            fixBtn.style.fontSize = 10;
            fixBtn.style.paddingLeft = 10;
            fixBtn.style.paddingRight = 10;
            fixBtn.style.backgroundColor = new Color(0.25f, 0.45f, 0.75f);
            fixBtn.style.color = Color.white;
            fixBtn.style.borderTopLeftRadius = 4;
            fixBtn.style.borderTopRightRadius = 4;
            fixBtn.style.borderBottomLeftRadius = 4;
            fixBtn.style.borderBottomRightRadius = 4;
            toast.Add(fixBtn);

            var closeBtn = new Button(() => toast.RemoveFromHierarchy()) { text = "x" };
            closeBtn.style.width = 20;
            closeBtn.style.height = 20;
            closeBtn.style.fontSize = 9;
            closeBtn.style.marginLeft = 4;
            closeBtn.style.paddingLeft = 0;
            closeBtn.style.paddingRight = 0;
            toast.Add(closeBtn);

            rootVisualElement.Add(toast);
            _compileToast = toast;

            rootVisualElement.schedule.Execute(() =>
            {
                if (toast.parent != null) toast.RemoveFromHierarchy();
            }).StartingIn(15000);

            Repaint();
        }

        public void StartFixSessionForCompileErrors(System.Collections.Generic.List<Context.ConsoleError> errors)
        {
            if (errors == null || errors.Count == 0) return;
            if (_session == null || _session.IsProcessing)
            {
                ShowToast("진행 중인 응답이 있어요. 끝난 뒤 다시 시도해주세요.");
                return;
            }

            _session.ClearHistory();
            _messageContainer.Clear();
            _attachmentBar.Clear();

            // Attach the distinct files involved (up to 3 to keep context light).
            var attached = new System.Collections.Generic.HashSet<string>();
            foreach (var e in errors)
            {
                if (string.IsNullOrEmpty(e.FilePath)) continue;
                if (attached.Count >= 3) break;
                if (attached.Add(e.FilePath))
                    _attachmentBar.Add(e.FilePath);
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("다음 Unity 컴파일 에러들을 분석하고 수정해줘. 각 에러의 원인을 설명하고, 해당 파일을 Edit 도구로 고쳐줘.");
            sb.AppendLine();
            sb.AppendLine("[Compile Errors]");
            int listed = 0;
            foreach (var e in errors)
            {
                sb.AppendLine($"- {e.Message}");
                if (++listed >= 15)
                {
                    if (errors.Count > listed)
                        sb.AppendLine($"... 외 {errors.Count - listed}개");
                    break;
                }
            }

            SetInputText(sb.ToString());
            rootVisualElement.schedule.Execute(OnSendClicked).StartingIn(50);
        }

        void LoadSession(SessionRecord record)
        {
            _session.LoadFromRecord(record);
            ReloadMessageBubbles();
            RefreshSidebar();
            Repaint();
        }

        /// <summary>
        /// Triggered by the Tools/Claude Code/Analyze Profiler Data menu item.
        /// Pre-fills the input with the snapshot text + an analysis prompt and auto-sends.
        /// </summary>
        public void StartProfilerAnalysisSession(string snapshotText)
        {
            if (_session == null) return;
            if (_session.IsProcessing)
            {
                ShowToast("진행 중인 응답이 있어요. 끝난 뒤 다시 시도해주세요.");
                return;
            }

            var prompt =
                "다음은 Unity Profiler가 캡처한 현재까지의 데이터다. " +
                "전반적인 상황을 한국어로 평가해주고, 의심되는 병목/스파이크/메모리 이슈가 있으면 짚어줘. " +
                "EditorLoop는 에디터 오버헤드라 실제 빌드에선 사라지니, PlayerLoop와 사용자 코드 위주로 봐주고. " +
                "근거 없는 추측은 \"확실하지 않음\"이라고 명시해줘.\n\n" +
                "```\n" + (snapshotText ?? "(empty)") + "\n```";

            SetInputText(prompt);
            Repaint();
            rootVisualElement.schedule.Execute(OnSendClicked).StartingIn(50);
        }

        void DeleteSession(string sessionId)
        {
            HistoryStorage.Delete(sessionId);
            if (sessionId == _session.SessionId)
                StartNewSession();
            else
                RefreshSidebar();
        }

        void StartNewSession()
        {
            _session.ClearHistory();
            _messageContainer.Clear();
            SessionSerializer.instance.ClearState();
            RefreshSidebar();
        }

        void ReloadMessageBubbles()
        {
            _messageContainer.Clear();
            for (int i = 0; i < _session.Messages.Count; i++)
                AddMessageBubble(_session.Messages[i].role, _session.Messages[i].content, i);
        }

        void BuildToolbar(VisualElement root)
        {
            _statusBar = new VisualElement();
            _statusBar.style.flexDirection = FlexDirection.Row;
            _statusBar.style.alignItems = Align.Center;
            _statusBar.style.justifyContent = Justify.SpaceBetween;
            _statusBar.style.paddingLeft = 10;
            _statusBar.style.paddingRight = 8;
            _statusBar.style.paddingTop = 4;
            _statusBar.style.paddingBottom = 4;
            _statusBar.style.minHeight = 34;
            _statusBar.style.flexShrink = 0;
            _statusBar.style.backgroundColor = new Color(0.13f, 0.13f, 0.13f);
            _statusBar.style.borderBottomWidth = 1;
            _statusBar.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);

            var statusGroup = new VisualElement();
            statusGroup.style.flexDirection = FlexDirection.Row;
            statusGroup.style.alignItems = Align.Center;

            _statusDot = new VisualElement();
            _statusDot.style.width = 8;
            _statusDot.style.height = 8;
            _statusDot.style.borderTopLeftRadius = 4;
            _statusDot.style.borderTopRightRadius = 4;
            _statusDot.style.borderBottomLeftRadius = 4;
            _statusDot.style.borderBottomRightRadius = 4;
            _statusDot.style.marginRight = 6;
            _statusDot.style.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
            statusGroup.Add(_statusDot);

            _statusLabel = new Label("Checking...");
            _statusLabel.style.fontSize = 11;
            _statusLabel.style.color = new Color(0.65f, 0.65f, 0.65f);
            statusGroup.Add(_statusLabel);

            _profileBadge = new VisualElement();
            _profileBadge.style.flexDirection = FlexDirection.Row;
            _profileBadge.style.alignItems = Align.Center;
            _profileBadge.style.marginLeft = 10;
            _profileBadge.style.paddingLeft = 6;
            _profileBadge.style.paddingRight = 8;
            _profileBadge.style.paddingTop = 2;
            _profileBadge.style.paddingBottom = 2;
            _profileBadge.style.backgroundColor = new Color(0.45f, 0.20f, 0.20f);
            _profileBadge.style.borderTopLeftRadius = 10;
            _profileBadge.style.borderTopRightRadius = 10;
            _profileBadge.style.borderBottomLeftRadius = 10;
            _profileBadge.style.borderBottomRightRadius = 10;
            _profileBadge.style.display = DisplayStyle.None;
            var recDot = new VisualElement();
            recDot.style.width = 6;
            recDot.style.height = 6;
            recDot.style.borderTopLeftRadius = 3;
            recDot.style.borderTopRightRadius = 3;
            recDot.style.borderBottomLeftRadius = 3;
            recDot.style.borderBottomRightRadius = 3;
            recDot.style.backgroundColor = new Color(1f, 0.4f, 0.4f);
            recDot.style.marginRight = 5;
            _profileBadge.Add(recDot);
            _profileBadgeLabel = new Label("REC 0.0s");
            _profileBadgeLabel.style.fontSize = 10;
            _profileBadgeLabel.style.color = Color.white;
            _profileBadgeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _profileBadge.Add(_profileBadgeLabel);
            _profileBadge.tooltip = "Profile capture가 진행 중입니다. /profile stop 로 종료하세요.";
            _profileBadge.RegisterCallback<MouseDownEvent>(_ => ExecuteProfileCommand("stop"));
            statusGroup.Add(_profileBadge);

            _updateBadge = new Button(() => UpdateChecker.OpenPackageManager());
            _updateBadge.style.height = 20;
            _updateBadge.style.marginLeft = 10;
            _updateBadge.style.paddingLeft = 8;
            _updateBadge.style.paddingRight = 8;
            _updateBadge.style.fontSize = 10;
            _updateBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
            _updateBadge.style.backgroundColor = new Color(0.16f, 0.42f, 0.30f);
            _updateBadge.style.color = new Color(0.85f, 1f, 0.9f);
            _updateBadge.style.borderTopLeftRadius = 10;
            _updateBadge.style.borderTopRightRadius = 10;
            _updateBadge.style.borderBottomLeftRadius = 10;
            _updateBadge.style.borderBottomRightRadius = 10;
            _updateBadge.style.display = DisplayStyle.None;
            statusGroup.Add(_updateBadge);

            _statusBar.Add(statusGroup);

            var buttonGroup = new VisualElement();
            buttonGroup.style.flexDirection = FlexDirection.Row;
            buttonGroup.style.alignItems = Align.Center;

            _accountButton = new Button(ShowAccountPopup);
            _accountButton.style.fontSize = 11;
            _accountButton.style.height = 20;
            _accountButton.style.paddingLeft = 8;
            _accountButton.style.paddingRight = 8;
            _accountButton.style.marginRight = 6;
            UpdateAccountButtonLabel();
            buttonGroup.Add(_accountButton);

            var modelDropdown = new PopupField<string>(
                ModelManager.GetDisplayNames(),
                Mathf.Max(0, ModelManager.GetCurrentIndex()));
            modelDropdown.style.fontSize = 11;
            modelDropdown.style.height = 20;
            modelDropdown.style.marginRight = 6;
            modelDropdown.style.minWidth = 110;
            modelDropdown.RegisterValueChangedCallback(evt =>
            {
                ModelManager.SetByDisplayName(evt.newValue);
            });
            buttonGroup.Add(modelDropdown);

            _permissionButton = new Button(ShowPermissionMenu) { text = "" };
            _permissionButton.style.fontSize = 11;
            _permissionButton.style.height = 20;
            _permissionButton.style.marginRight = 6;
            _permissionButton.style.minWidth = 140;
            _permissionButton.style.paddingLeft = 6;
            _permissionButton.style.paddingRight = 6;
            _permissionButton.style.flexDirection = FlexDirection.Row;
            _permissionButton.style.alignItems = Align.Center;
            _permissionButton.style.justifyContent = Justify.SpaceBetween;
            RefreshPermissionButton();
            buttonGroup.Add(_permissionButton);

            _claudeMdButton = new Button(ShowClaudeMdMenu) { text = "" };
            _claudeMdButton.style.fontSize = 11;
            _claudeMdButton.style.height = 20;
            _claudeMdButton.style.paddingLeft = 6;
            _claudeMdButton.style.paddingRight = 6;
            _claudeMdButton.style.marginRight = 6;
            _claudeMdButton.style.flexDirection = FlexDirection.Row;
            _claudeMdButton.style.alignItems = Align.Center;
            {
                var bookIcon = VectorIcons.Make(IconType.BookOpen, 12);
                bookIcon.style.marginRight = 6;
                _claudeMdButton.Add(bookIcon);
                var lbl = new Label("CLAUDE.md");
                lbl.style.fontSize = 11;
                lbl.pickingMode = PickingMode.Ignore;
                _claudeMdButton.Add(lbl);
                var chev = VectorIcons.Make(IconType.ChevronDown, 10);
                chev.style.marginLeft = 4;
                _claudeMdButton.Add(chev);
            }
            buttonGroup.Add(_claudeMdButton);

            _errorBadge = new Button(ShowErrorDropdown);
            _errorBadge.style.fontSize = 11;
            _errorBadge.style.height = 20;
            _errorBadge.style.paddingLeft = 6;
            _errorBadge.style.paddingRight = 6;
            _errorBadge.style.marginRight = 6;
            _errorBadge.style.backgroundColor = new Color(0.5f, 0.18f, 0.18f);
            _errorBadge.style.color = Color.white;
            _errorBadge.style.borderTopLeftRadius = 10;
            _errorBadge.style.borderTopRightRadius = 10;
            _errorBadge.style.borderBottomLeftRadius = 10;
            _errorBadge.style.borderBottomRightRadius = 10;
            _errorBadge.style.display = DisplayStyle.None;
            buttonGroup.Add(_errorBadge);

            _historyToggleButton = new Button(ToggleSidebar) { text = "History »" };
            _historyToggleButton.style.fontSize = 11;
            _historyToggleButton.style.height = 20;
            _historyToggleButton.style.paddingLeft = 8;
            _historyToggleButton.style.paddingRight = 8;
            _historyToggleButton.style.marginRight = 4;
            buttonGroup.Add(_historyToggleButton);

            _clearButton = new Button(OnClearClicked) { text = "Clear" };
            _clearButton.style.fontSize = 11;
            _clearButton.style.height = 20;
            _clearButton.style.paddingLeft = 8;
            _clearButton.style.paddingRight = 8;
            buttonGroup.Add(_clearButton);

            _statusBar.Add(buttonGroup);

            root.Add(_statusBar);
        }

        void BuildMessageArea(VisualElement root)
        {
            _messageContainer = new ScrollView(ScrollViewMode.Vertical);
            _messageContainer.style.flexGrow = 1;
            _messageContainer.style.paddingLeft = 12;
            _messageContainer.style.paddingRight = 12;
            _messageContainer.style.paddingTop = 12;
            _messageContainer.style.paddingBottom = 12;
            root.Add(_messageContainer);
        }

        void BuildInputArea(VisualElement root)
        {
            _inputArea = new VisualElement();
            _inputArea.style.flexShrink = 0;
            _inputArea.style.borderTopWidth = 1;
            _inputArea.style.borderTopColor = new Color(0.25f, 0.25f, 0.25f);
            _inputArea.style.paddingLeft = 10;
            _inputArea.style.paddingRight = 10;
            _inputArea.style.paddingTop = 8;
            _inputArea.style.paddingBottom = 10;
            _inputArea.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);

            _attachmentBar = new AttachmentBar();
            _inputArea.Add(_attachmentBar.Build());

            var inputRow = new VisualElement();
            inputRow.style.flexDirection = FlexDirection.Row;
            inputRow.style.alignItems = Align.FlexEnd;

            var attachBtn = new Button(ShowAttachmentMenu) { text = "+" };
            attachBtn.style.width = 32;
            attachBtn.style.height = 36;
            attachBtn.style.flexShrink = 0;
            attachBtn.style.marginRight = 6;
            attachBtn.style.fontSize = 16;
            attachBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            attachBtn.style.backgroundColor = new Color(0.25f, 0.25f, 0.28f);
            attachBtn.style.color = new Color(0.85f, 0.85f, 0.85f);
            attachBtn.style.borderTopLeftRadius = 6;
            attachBtn.style.borderTopRightRadius = 6;
            attachBtn.style.borderBottomLeftRadius = 6;
            attachBtn.style.borderBottomRightRadius = 6;
            inputRow.Add(attachBtn);

            _inputField = new TextField();
            _inputField.multiline = true;
            _inputField.style.flexGrow = 1;
            _inputField.style.flexShrink = 1;
            _inputField.style.flexBasis = 0;
            _inputField.style.minWidth = 0;
            _inputField.style.minHeight = 36;
            _inputField.style.maxHeight = 100;
            _inputField.style.marginRight = 6;

            var textInput = _inputField.Q<VisualElement>("unity-text-input");
            if (textInput != null)
            {
                textInput.style.paddingLeft = 10;
                textInput.style.paddingRight = 10;
                textInput.style.paddingTop = 8;
                textInput.style.paddingBottom = 8;
                textInput.style.borderTopLeftRadius = 6;
                textInput.style.borderTopRightRadius = 6;
                textInput.style.borderBottomLeftRadius = 6;
                textInput.style.borderBottomRightRadius = 6;
                textInput.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
                textInput.style.borderTopWidth = 1;
                textInput.style.borderBottomWidth = 1;
                textInput.style.borderLeftWidth = 1;
                textInput.style.borderRightWidth = 1;
                textInput.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
                textInput.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                textInput.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
                textInput.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            }

            _inputField.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
            _inputField.RegisterCallback<ValidateCommandEvent>(OnValidateCommand, TrickleDown.TrickleDown);
            _inputField.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand, TrickleDown.TrickleDown);
            _inputField.RegisterValueChangedCallback(OnInputValueChanged);
            inputRow.Add(_inputField);

            _effortButton = new Button(ShowEffortPopup) { text = "" };
            _effortButton.style.height = 36;
            _effortButton.style.flexShrink = 0;
            _effortButton.style.marginRight = 6;
            _effortButton.style.paddingLeft = 8;
            _effortButton.style.paddingRight = 8;
            _effortButton.style.fontSize = 11;
            _effortButton.style.backgroundColor = new Color(0.25f, 0.25f, 0.28f);
            _effortButton.style.color = new Color(0.85f, 0.85f, 0.85f);
            _effortButton.style.borderTopLeftRadius = 6;
            _effortButton.style.borderTopRightRadius = 6;
            _effortButton.style.borderBottomLeftRadius = 6;
            _effortButton.style.borderBottomRightRadius = 6;
            RefreshEffortButton();
            inputRow.Add(_effortButton);

            _usageIndicator = new UsageIndicator();
            _usageIndicator.style.marginRight = 6;
            _usageIndicator.style.marginBottom = 4;
            _usageIndicator.SetIdle("Context usage will appear after first response");
            inputRow.Add(_usageIndicator);

            _sendButton = new Button(OnSendClicked) { text = "Send" };
            _sendButton.style.width = 56;
            _sendButton.style.height = 36;
            _sendButton.style.flexShrink = 0;
            _sendButton.style.borderTopLeftRadius = 6;
            _sendButton.style.borderTopRightRadius = 6;
            _sendButton.style.borderBottomLeftRadius = 6;
            _sendButton.style.borderBottomRightRadius = 6;
            _sendButton.style.backgroundColor = new Color(0.25f, 0.45f, 0.75f);
            _sendButton.style.color = Color.white;
            inputRow.Add(_sendButton);

            _stopButton = new Button(OnStopClicked) { text = "Stop" };
            _stopButton.style.width = 56;
            _stopButton.style.height = 36;
            _stopButton.style.flexShrink = 0;
            _stopButton.style.borderTopLeftRadius = 6;
            _stopButton.style.borderTopRightRadius = 6;
            _stopButton.style.borderBottomLeftRadius = 6;
            _stopButton.style.borderBottomRightRadius = 6;
            _stopButton.style.backgroundColor = new Color(0.65f, 0.25f, 0.25f);
            _stopButton.style.color = Color.white;
            _stopButton.style.display = DisplayStyle.None;
            inputRow.Add(_stopButton);

            _inputArea.Add(inputRow);

            var hintLabel = new Label("Enter to send, Shift+Enter for newline");
            hintLabel.style.fontSize = 10;
            hintLabel.style.color = new Color(0.45f, 0.45f, 0.45f);
            hintLabel.style.marginTop = 4;
            _inputArea.Add(hintLabel);

            root.Add(_inputArea);
        }

        void OnInputKeyDown(KeyDownEvent evt)
        {
            if (_tagPopup != null && _tagPopup.IsOpen)
            {
                if (_tagPopup.HandleKey(evt))
                {
                    evt.PreventDefault();
                    evt.StopPropagation();
                    return;
                }
            }

            if (_slashPopup != null && _slashPopup.IsOpen)
            {
                if (evt.keyCode == KeyCode.Return && !evt.shiftKey)
                {
                    var cmd = _slashPopup.SelectedCommand;
                    if (cmd != null)
                    {
                        evt.PreventDefault();
                        evt.StopPropagation();
                        ExecuteSlashCommand(cmd, "");
                        return;
                    }
                }

                if (_slashPopup.HandleKey(evt))
                {
                    evt.PreventDefault();
                    evt.StopPropagation();
                    return;
                }
            }

            if (evt.keyCode == KeyCode.Return && !evt.shiftKey)
            {
                evt.PreventDefault();
                evt.StopPropagation();
                OnSendClicked();
                return;
            }

            if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.V)
            {
                if (TryPasteImageAttachment())
                {
                    evt.PreventDefault();
                    evt.StopPropagation();
                }
            }
        }

        void OnInputValueChanged(ChangeEvent<string> evt)
        {
            var text = evt.newValue ?? "";

            // Slash command (only at message start)
            var firstWord = ExtractFirstWord(text);
            if (text.StartsWith("/") && !text.Contains("\n"))
            {
                if (!_slashPopup.IsOpen) _slashPopup.Open();
                _slashPopup.UpdateFilter(firstWord);
            }
            else
            {
                if (_slashPopup.IsOpen) _slashPopup.Close();
            }

            // @ tag detection - check current cursor/end position
            int atIdx = FindCurrentAtIndex(text);
            if (atIdx >= 0)
            {
                _tagStartIndex = atIdx;
                var query = text.Substring(atIdx + 1);
                if (!_tagPopup.IsOpen) _tagPopup.Open();
                _tagPopup.UpdateFilter(query);
            }
            else
            {
                _tagStartIndex = -1;
                if (_tagPopup.IsOpen) _tagPopup.Close();
            }
        }

        static int FindCurrentAtIndex(string text)
        {
            if (string.IsNullOrEmpty(text)) return -1;
            // Find the last '@' that is at start or preceded by whitespace,
            // and followed by no whitespace until end.
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (char.IsWhiteSpace(text[i])) return -1;
                if (text[i] == '@')
                {
                    if (i == 0 || char.IsWhiteSpace(text[i - 1])) return i;
                    return -1;
                }
            }
            return -1;
        }

        void OnTagSelected(ContextTagItem item)
        {
            // Remove the "@query" portion from the input text
            if (_tagStartIndex >= 0 && _tagStartIndex <= _inputField.value.Length)
            {
                var before = _inputField.value.Substring(0, _tagStartIndex);
                var afterStart = _tagStartIndex + 1;
                int spaceIdx = _inputField.value.IndexOf(' ', afterStart);
                var after = spaceIdx < 0 ? "" : _inputField.value.Substring(spaceIdx);
                SetInputText((before + after).TrimEnd() + " ");
            }
            _tagStartIndex = -1;

            // Add as attachment chip
            var att = new Attachment
            {
                Kind = item.Kind,
                Identifier = item.Identifier,
                DisplayName = item.DisplayName
            };
            _attachmentBar.AddAttachment(att);
            _inputField.Focus();
            Repaint();
        }

        static string ExtractFirstWord(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int spaceIdx = text.IndexOf(' ');
            return spaceIdx < 0 ? text : text.Substring(0, spaceIdx);
        }

        void OnSlashCommandSelected(SlashCommand cmd)
        {
            // Tab/Click: insert command name, leave open for args if needed
            SetInputText(cmd.Name + (cmd.TakesArguments ? " " : ""));
            if (!cmd.TakesArguments) _slashPopup.Close();
            _inputField.Focus();
        }

        void ExecuteSlashCommand(SlashCommand cmd, string args)
        {
            _slashPopup.Close();
            SetInputText("");

            try
            {
                cmd.Execute?.Invoke(this, args);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ClaudeCode] Slash command failed: {e.Message}");
            }
        }

        void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (evt.commandName == "Paste")
                evt.StopPropagation();
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (evt.commandName != "Paste") return;
            if (TryPasteImageAttachment())
                evt.StopPropagation();
        }

        bool TryPasteImageAttachment()
        {
            var imagePath = ClipboardImage.TryPasteImage();
            if (string.IsNullOrEmpty(imagePath)) return false;

            _attachmentBar.Add(imagePath);
            Repaint();
            return true;
        }

        void AddAttachment(string path)
        {
            _attachmentBar.Add(path);
            Repaint();
        }

        void OnSendClicked()
        {
            var text = _inputField.value?.Trim() ?? "";
            var attachments = _attachmentBar.Items;
            if (string.IsNullOrEmpty(text) && attachments.Count == 0) return;
            if (_session.IsProcessing) return;

            if (text.StartsWith("/"))
            {
                var firstWord = ExtractFirstWord(text);
                var cmd = SlashCommandRegistry.FindExact(firstWord);
                if (cmd != null)
                {
                    var args = text.Length > firstWord.Length ? text.Substring(firstWord.Length).TrimStart() : "";
                    ExecuteSlashCommand(cmd, args);
                    return;
                }
            }

            var fullMessage = BuildFullMessageForCli(text, attachments);

            AddMessageBubble("user", BuildUserDisplayMessage(text, attachments));
            SetInputText("");
            _attachmentBar.Clear();
            _inputField.Focus();

            ShowStreamingState();
            _session.SendMessage(fullMessage);
        }

        static string BuildFullMessageForCli(string text, IReadOnlyList<Attachment> attachments)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(text)) sb.Append(text);

            foreach (var att in attachments)
            {
                var snippet = BuildAttachmentSnippet(att);
                if (string.IsNullOrEmpty(snippet)) continue;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(snippet);
            }
            return sb.ToString();
        }

        static string BuildAttachmentSnippet(Attachment att)
        {
            switch (att.Kind)
            {
                case AttachmentKind.File:
                case AttachmentKind.Folder:
                case AttachmentKind.Script:
                case AttachmentKind.Prefab:
                case AttachmentKind.Material:
                case AttachmentKind.Image:
                    return "@" + att.Identifier;

                case AttachmentKind.GameObject:
                    return BuildGameObjectSnippetFromPath(att.Identifier);

                case AttachmentKind.Scene:
                    return Context.SceneContextProvider.GetActiveSceneInfo();

                case AttachmentKind.Selection:
                    return Context.SceneContextProvider.GetSelectedObjectsInfo();

                case AttachmentKind.ConsoleErrors:
                    return Context.ConsoleLogProvider.GetRecentErrors();

                case AttachmentKind.ProjectStructure:
                    return Context.ProjectStructureProvider.GetAssetsStructure();
            }
            return null;
        }

        static string BuildGameObjectSnippetFromPath(string path)
        {
            var go = GameObject.Find(path);
            if (go == null) return $"[Attached: GameObject {path} — not found]";
            return BuildGameObjectSnippet(go);
        }

        static string BuildUserDisplayMessage(string text, IReadOnlyList<Attachment> attachments)
        {
            if (attachments.Count == 0) return text;
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(text)) { sb.Append(text); sb.Append('\n'); }
            foreach (var att in attachments)
                sb.Append(att.Icon).Append(' ').AppendLine(att.DisplayName);
            return sb.ToString().TrimEnd();
        }

        void OnStopClicked()
        {
            _session.StopGeneration();
            StopTypingAnimation();
            ClearStreamingBubble();
            SetIdleState();
        }

        void ClearStreamingBubble()
        {
            if (_streamingRow != null)
            {
                _streamingRow.parent?.Remove(_streamingRow);
                _streamingRow = null;
            }
            else if (_streamingBubble != null)
            {
                _streamingBubble.parent?.Remove(_streamingBubble);
            }
            _streamingBubble = null;
            _streamingLabel = null;
            _typingIndicator = null;
            _streamingTimerLabel = null;
        }

        void OnClearClicked()
        {
            _session.ClearHistory();
            _messageContainer.Clear();
            SessionSerializer.instance.ClearState();
        }

        void ShowStreamingState()
        {
            LockReloadIfNeeded();

            _sendButton.style.display = DisplayStyle.None;
            _stopButton.style.display = DisplayStyle.Flex;
            _statusDot.style.backgroundColor = new Color(0.9f, 0.7f, 0.2f);

            // 응답 중에는 입력란 비활성화
            if (_inputField != null)
            {
                _inputField.SetEnabled(false);
                _inputField.style.opacity = 0.6f;
            }

            _streamStartTime = DateTime.Now;
            _currentToolName = null;
            StartStreamTimer();

            _streamingRow = new VisualElement();
            _streamingRow.style.flexDirection = FlexDirection.Row;
            _streamingRow.style.alignItems = Align.FlexEnd;
            _streamingRow.style.alignSelf = Align.FlexStart;
            _streamingRow.style.maxWidth = Length.Percent(95);
            _streamingRow.style.marginBottom = 10;

            _streamingBubble = CreateBubble("assistant");
            _streamingBubble.style.marginBottom = 0;
            _streamingBubble.style.alignSelf = Align.Stretch;
            _streamingBubble.style.flexShrink = 1;

            _typingIndicator = new Label("●");
            _typingIndicator.style.fontSize = 14;
            _typingIndicator.style.color = new Color(0.6f, 0.6f, 0.6f);
            _typingIndicator.style.unityFontStyleAndWeight = FontStyle.Bold;
            _streamingBubble.Add(_typingIndicator);

            _streamingLabel = new Label("");
            _streamingLabel.style.whiteSpace = WhiteSpace.Normal;
            _streamingLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
            _streamingLabel.style.display = DisplayStyle.None;
            _streamingBubble.Add(_streamingLabel);

            _streamingRow.Add(_streamingBubble);

            _streamingTimerLabel = new Label("0s");
            _streamingTimerLabel.style.fontSize = 10;
            _streamingTimerLabel.style.color = new Color(0.55f, 0.6f, 0.7f);
            _streamingTimerLabel.style.marginLeft = 10;
            _streamingTimerLabel.style.marginBottom = 6;
            _streamingTimerLabel.style.flexShrink = 0;
            _streamingTimerLabel.style.alignSelf = Align.FlexEnd;
            _streamingRow.Add(_streamingTimerLabel);

            _messageContainer.Add(_streamingRow);
            _firstDeltaReceived = false;
            StartTypingAnimation();

            ScrollToBottom();
        }

        void StartStreamTimer()
        {
            _streamTimer?.Pause();
            UpdateStreamStatusLabel();
            _streamTimer = rootVisualElement.schedule.Execute(UpdateStreamStatusLabel).Every(1000);
        }

        void StopStreamTimer()
        {
            _streamTimer?.Pause();
            _streamTimer = null;
            _currentToolName = null;
        }

        void UpdateStreamStatusLabel()
        {
            var elapsed = DateTime.Now - _streamStartTime;
            var elapsedStr = FormatElapsed(elapsed);

            if (_statusLabel != null)
            {
                if (!string.IsNullOrEmpty(_currentToolName))
                    _statusLabel.text = $"{_currentToolName} · {elapsedStr}";
                else
                    _statusLabel.text = $"Generating... {elapsedStr}";
            }

            if (_streamingTimerLabel != null)
            {
                if (!string.IsNullOrEmpty(_currentToolName))
                    _streamingTimerLabel.text = $"{_currentToolName} · {elapsedStr}";
                else
                    _streamingTimerLabel.text = elapsedStr;
            }
        }

        static string FormatElapsed(TimeSpan span)
        {
            int totalSec = (int)span.TotalSeconds;
            if (totalSec < 60) return $"{totalSec}s";
            return $"{totalSec / 60}m {totalSec % 60}s";
        }

        void StartTypingAnimation()
        {
            _typingDotsFrame = 0;
            _typingAnimator?.Pause();
            _typingAnimator = rootVisualElement.schedule.Execute(() =>
            {
                if (_typingIndicator == null) return;
                _typingDotsFrame = (_typingDotsFrame + 1) % 4;
                _typingIndicator.text = _typingDotsFrame switch
                {
                    0 => "●",
                    1 => "● ●",
                    2 => "● ● ●",
                    _ => "● ●"
                };
            }).Every(300);
        }

        void StopTypingAnimation()
        {
            _typingAnimator?.Pause();
            _typingAnimator = null;
            if (_typingIndicator != null)
            {
                _typingIndicator.style.display = DisplayStyle.None;
            }
            if (_streamingLabel != null)
            {
                _streamingLabel.style.display = DisplayStyle.Flex;
            }
        }

        void HandleTextDelta(string delta)
        {
            if (_streamingLabel != null)
            {
                if (!_firstDeltaReceived)
                {
                    _firstDeltaReceived = true;
                    StopTypingAnimation();
                }
                _streamingLabel.text += delta;
                ScrollToBottom();
                Repaint();
            }
        }

        void HandleMessageComplete(ChatMessage msg)
        {
            StopTypingAnimation();
            ClearStreamingBubble();

            AddMessageBubble("assistant", msg.content);
            SetIdleState();
            SaveSession();

            // 응답 완료 후 한 번 더 Refresh — Bash 등 권한 시스템을 거치지 않는 도구로
            // 파일이 생성된 경우를 위한 안전망
            AssetDatabase.Refresh();
        }

        void HandleToolUse(ToolUseInfo toolUse)
        {
            _currentToolName = $"Tool: {toolUse.name}";
            UpdateStreamStatusLabel();
        }

        void UpdateAccountButtonLabel()
        {
            if (_accountButton == null) return;
            var info = _authManager.GetAccountInfo();
            if (info != null && info.loggedIn && !string.IsNullOrEmpty(info.email))
            {
                var email = info.email;
                var shortEmail = email.Length > 18 ? email.Substring(0, 16) + ".." : email;
                _accountButton.text = shortEmail;
            }
            else
            {
                _accountButton.text = "Sign in";
            }
        }

        void ShowAccountPopup()
        {
            var popup = AccountPopup.Build(_authManager,
                onLogout: () =>
                {
                    UpdateAccountButtonLabel();
                    UpdateConnectionStatus();
                    Repaint();
                },
                onRefresh: () =>
                {
                    UpdateAccountButtonLabel();
                    UpdateConnectionStatus();
                    Repaint();
                },
                onLoginRequested: ShowLoginDialog);
            rootVisualElement.Add(popup);
            Repaint();
        }

        public void ExecuteClearCommand()
        {
            _session.ClearHistory();
            _messageContainer.Clear();
            SessionSerializer.instance.ClearState();
            _attachmentBar.Clear();
            RefreshSidebar();
        }

        public void ExecuteToggleSidebarCommand() => ToggleSidebar();

        public void ExecuteLoginCommand() => ShowLoginDialog();

        public void ExecuteLogoutCommand()
        {
            if (EditorUtility.DisplayDialog("Logout", "로그아웃 하시겠습니까?", "Logout", "Cancel"))
            {
                _authManager.Logout();
                UpdateConnectionStatus();
                UpdateAccountButtonLabel();
            }
        }

        public void ExecuteCopyLastCommand()
        {
            for (int i = _session.Messages.Count - 1; i >= 0; i--)
            {
                if (_session.Messages[i].role == "assistant")
                {
                    GUIUtility.systemCopyBuffer = _session.Messages[i].content;
                    ShowToast("마지막 응답을 클립보드에 복사했습니다.");
                    return;
                }
            }
            ShowToast("복사할 응답이 없습니다.");
        }

        public void ExecuteModelCommand(string args)
        {
            var name = args?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ShowToast($"현재 모델: {ModelManager.CurrentDisplayName}. 사용법: /model opus");
                return;
            }

            ModelManager.SetByDisplayName(name);
            var opt = ModelManager.Options.Find(o =>
                o.CliValue.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                o.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (opt != null) ModelManager.CurrentModel = opt.CliValue;
            ShowToast($"모델 변경: {ModelManager.CurrentDisplayName}");
            BuildUI();
            RestoreSession();
        }

        public void ExecuteSettingsCommand() => SettingsService.OpenProjectSettings("Project/Claude Code");

        public void ExecuteHelpCommand()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("사용 가능한 슬래시 명령어:\n");
            foreach (var c in SlashCommandRegistry.All)
                sb.AppendLine($"{c.Icon} {c.Name} — {c.Description}");
            EditorUtility.DisplayDialog("Claude Code Help", sb.ToString(), "OK");
        }

        public void ExecuteProfileCommand(string args)
        {
            var sub = (args ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(sub)) sub = MCP.Tools.ProfilerSession.IsActive ? "stop" : "start";

            switch (sub)
            {
                case "start":
                    MCP.Tools.ProfilerSession.Start();
                    ShowToast($"Profile capture 시작 (Play Mode: {(EditorApplication.isPlaying ? "running" : "OFF")})");
                    break;

                case "stop":
                    if (!MCP.Tools.ProfilerSession.IsActive)
                    {
                        ShowToast("Profile capture가 활성화되어 있지 않습니다.");
                        return;
                    }
                    var result = MCP.Tools.ProfilerSession.Stop();
                    var text = MCP.Tools.ProfilerSession.FormatResultAsText(result);
                    // Render as an assistant-style bubble for readability. This is UI-only
                    // (not added to SessionManager.Messages) so it won't pollute Claude's context.
                    // User can copy/paste or ask "이 결과 분석해줘" — Claude will then call
                    // unity_profile_stop again (which will return "no active" since we just stopped),
                    // so practically: tell user it's also available via the MCP tool flow.
                    AddMessageBubble("assistant", "```\n" + text + "```");
                    ShowToast("Profile capture 종료. 결과를 채팅에 표시했습니다.");
                    Repaint();
                    break;

                case "status":
                    var status = MCP.Tools.ProfilerSession.IsActive
                        ? $"Capture active · {MCP.Tools.ProfilerSession.ElapsedSeconds:F1}s elapsed"
                        : "No active capture.";
                    ShowToast(status);
                    break;

                default:
                    ShowToast("사용법: /profile start | stop | status");
                    break;
            }
        }

        // Safely replace input field text. Direct `_inputField.value = ...` leaves the
        // UI Toolkit multiline text editor's internal cursor/selection indices pointing
        // at their old position, which then throws ArgumentOutOfRangeException on the
        // next keypress (TextEditingUtilities.Insert with stale startIndex).
        // Workaround: clamp the cursor before the value change (always valid for old text),
        // set the new value, then re-clamp to the new end via public API + reflection
        // fallback, and finally trigger a Blur+Focus on the next frame which forces the
        // editor manipulator to re-initialize its state.
        void SetInputText(string text)
        {
            if (_inputField == null) return;
            var s = text ?? string.Empty;

            // 1) Pre-clamp cursor to 0 — valid for old string of any length.
            try { _inputField.SelectRange(0, 0); } catch { }

            // 2) Set the value (this triggers our ChangeEvent listener too).
            _inputField.value = s;

            // 3) Move cursor to new end via public API.
            int end = s.Length;
            try { _inputField.SelectRange(end, end); } catch { }

            // 4) Reflection fallback — reach into the inner TextElement and force-write
            //    cursorIndex / selectIndex. Works around 2022.3 multiline SelectRange quirks.
            ResetInnerTextCursor(end);

            // 5) Next frame, re-apply + Blur/Focus toggle to fully re-initialize editor state.
            _inputField.schedule.Execute(() =>
            {
                if (_inputField == null) return;
                try { _inputField.SelectRange(end, end); } catch { }
                ResetInnerTextCursor(end);

                bool hasFocus = false;
                try
                {
                    var fc = _inputField.focusController;
                    var focused = fc?.focusedElement as VisualElement;
                    hasFocus = focused != null && (focused == _inputField || _inputField.Contains(focused));
                }
                catch { }
                if (hasFocus)
                {
                    try { _inputField.Blur(); _inputField.Focus(); } catch { }
                }
            });
        }

        void ResetInnerTextCursor(int index)
        {
            try
            {
                var textInput = _inputField.Q<VisualElement>("unity-text-input");
                if (textInput == null) return;
                var textElement = textInput.Q<TextElement>();
                if (textElement == null) return;

                var selectionProp = textElement.GetType().GetProperty(
                    "selection",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var selection = selectionProp?.GetValue(textElement);
                if (selection == null) return;

                var t = selection.GetType();
                t.GetProperty("cursorIndex")?.SetValue(selection, index);
                t.GetProperty("selectIndex")?.SetValue(selection, index);
            }
            catch { /* fall through silently */ }
        }

        void ShowToast(string message)
        {
            var toast = new Label(message);
            toast.style.position = Position.Absolute;
            toast.style.bottom = 80;
            toast.style.left = Length.Percent(50);
            toast.style.translate = new Translate(Length.Percent(-50), 0);
            toast.style.backgroundColor = new Color(0.18f, 0.18f, 0.22f, 0.95f);
            toast.style.color = Color.white;
            toast.style.fontSize = 11;
            toast.style.paddingLeft = 14;
            toast.style.paddingRight = 14;
            toast.style.paddingTop = 8;
            toast.style.paddingBottom = 8;
            toast.style.borderTopLeftRadius = 16;
            toast.style.borderTopRightRadius = 16;
            toast.style.borderBottomLeftRadius = 16;
            toast.style.borderBottomRightRadius = 16;
            toast.style.borderTopWidth = 1;
            toast.style.borderBottomWidth = 1;
            toast.style.borderLeftWidth = 1;
            toast.style.borderRightWidth = 1;
            toast.style.borderTopColor = new Color(0.35f, 0.35f, 0.4f);
            toast.style.borderBottomColor = new Color(0.35f, 0.35f, 0.4f);
            toast.style.borderLeftColor = new Color(0.35f, 0.35f, 0.4f);
            toast.style.borderRightColor = new Color(0.35f, 0.35f, 0.4f);
            rootVisualElement.Add(toast);

            rootVisualElement.schedule.Execute(() => toast.RemoveFromHierarchy()).StartingIn(2000);
        }

        void ShowAttachmentMenu()
        {
            var menu = AttachmentMenu.Build(this, snippet =>
            {
                if (snippet.StartsWith("@"))
                {
                    _attachmentBar.Add(snippet.Substring(1));
                    Repaint();
                }
                else
                {
                    var current = _inputField.value ?? "";
                    if (current.Length > 0 && !current.EndsWith(" ") && !current.EndsWith("\n"))
                        current += " ";
                    SetInputText(current + snippet);
                    _inputField.Focus();
                }
            });
            rootVisualElement.Add(menu);
            Repaint();
        }

        void ShowLoginDialog()
        {
            var dialog = new LoginDialog(this, _authManager, () =>
            {
                _authManager.InvalidateCache();
                UpdateAccountButtonLabel();
                UpdateConnectionStatus();
                Repaint();
            });
            rootVisualElement.Add(dialog.Build());
            Repaint();
        }

        void HandlePermissionRequested(PermissionRequest request)
        {
            VisualElement dialog;
            if (DiffView.IsDiffableTool(request.ToolName))
            {
                dialog = DiffView.Build(request, decision =>
                {
                    request.Decision.Set(decision);
                    Repaint();
                });
            }
            else
            {
                dialog = PermissionDialog.Build(request, decision =>
                {
                    request.Decision.Set(decision);
                    Repaint();
                });
            }
            rootVisualElement.Add(dialog);
            Repaint();
            Focus();
        }

        void HandleUsageUpdated()
        {
            UpdateUsageIndicator();
        }

        void UpdateUsageIndicator()
        {
            if (_usageIndicator == null || _session == null) return;
            var u = _session.LatestUsage;
            var cw = _session.LatestContextWindow;
            if (u == null || cw <= 0)
            {
                _usageIndicator.SetIdle("Context usage will appear after first response");
                return;
            }
            float ratio = (float)u.TotalInput / cw;
            string tooltip = $"Context: {FormatTokens(u.TotalInput)} / {FormatTokens(cw)} ({Mathf.RoundToInt(ratio * 100)}%)";
            if (_session.TotalCostUsd > 0)
                tooltip += $"\nTotal cost: ${_session.TotalCostUsd:F4}";
            _usageIndicator.SetValue(ratio, tooltip);
        }

        static string FormatTokens(int tokens)
        {
            if (tokens >= 1_000_000) return $"{tokens / 1_000_000f:F2}M";
            if (tokens >= 1_000) return $"{tokens / 1_000f:F1}k";
            return tokens.ToString();
        }

        void HandleFileModificationApproved()
        {
            // Claude가 파일 쓰기 도구 실행 직후 Unity가 빠르게 인식하도록 트리거.
            // CLI가 실제로 파일을 디스크에 쓸 시간 확보 위해 짧은 지연 후 Refresh.
            rootVisualElement.schedule.Execute(() =>
            {
                AssetDatabase.Refresh();
            }).StartingIn(400);
        }

        void HandleError(string error)
        {
            // Fallback path: only invoked when OnDetailedError isn't wired
            HandleDetailedError(ErrorInfo.Classify(error));
        }

        void HandleDetailedError(ErrorInfo info)
        {
            StopTypingAnimation();
            ClearStreamingBubble();
            AddErrorBubble(info);
            SetIdleState();
        }

        void AddErrorBubble(ErrorInfo info)
        {
            var bubble = CreateBubble("error");

            var label = new Label(info.UserMessage);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = Color.white;
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 6;
            label.selection.isSelectable = true;
            bubble.Add(label);

            if (!string.IsNullOrEmpty(info.RawMessage) && info.RawMessage != info.UserMessage)
            {
                var foldout = new Foldout { text = "Details", value = false };
                foldout.style.color = new Color(0.9f, 0.9f, 0.9f);
                var raw = new Label(info.RawMessage);
                raw.style.whiteSpace = WhiteSpace.Normal;
                raw.style.color = new Color(0.85f, 0.85f, 0.85f);
                raw.style.fontSize = 10;
                raw.selection.isSelectable = true;
                foldout.Add(raw);
                bubble.Add(foldout);
            }

            if (!string.IsNullOrEmpty(info.ActionLabel))
            {
                var actionRow = new VisualElement();
                actionRow.style.flexDirection = FlexDirection.Row;
                actionRow.style.justifyContent = Justify.FlexEnd;
                actionRow.style.marginTop = 6;

                var actionBtn = new Button(() => HandleErrorAction(info)) { text = info.ActionLabel };
                actionBtn.style.height = 24;
                actionBtn.style.paddingLeft = 10;
                actionBtn.style.paddingRight = 10;
                actionBtn.style.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
                actionBtn.style.color = Color.white;
                actionBtn.style.borderTopLeftRadius = 4;
                actionBtn.style.borderTopRightRadius = 4;
                actionBtn.style.borderBottomLeftRadius = 4;
                actionBtn.style.borderBottomRightRadius = 4;
                actionRow.Add(actionBtn);

                bubble.Add(actionRow);
            }

            _messageContainer.Add(bubble);
            ScrollToBottom();
        }

        void HandleErrorAction(ErrorInfo info)
        {
            switch (info.Category)
            {
                case ErrorCategory.CliNotFound:
                    SettingsService.OpenProjectSettings("Project/Claude Code");
                    break;
                case ErrorCategory.NotAuthenticated:
                    ShowLoginDialog();
                    break;
                case ErrorCategory.SessionConflict:
                    _session.ClearHistory();
                    RetryLastMessage();
                    break;
                default:
                    if (info.Retryable) RetryLastMessage();
                    break;
            }
        }

        void RetryLastMessage()
        {
            var last = _session.LastUserMessage;
            if (string.IsNullOrEmpty(last)) return;
            SetInputText(last);
            OnSendClicked();
        }

        void SetIdleState()
        {
            UnlockReloadIfNeeded();
            StopStreamTimer();

            _sendButton.style.display = DisplayStyle.Flex;
            _stopButton.style.display = DisplayStyle.None;
            _statusDot.style.backgroundColor = new Color(0.3f, 0.7f, 0.3f);

            // 입력란 다시 활성화
            if (_inputField != null)
            {
                _inputField.SetEnabled(true);
                _inputField.style.opacity = 1f;
            }

            UpdateConnectionStatus();
        }

        VisualElement CreateBubble(string role)
        {
            var bubble = new VisualElement();
            bubble.style.marginBottom = 10;
            bubble.style.paddingLeft = 14;
            bubble.style.paddingRight = 14;
            bubble.style.paddingTop = 10;
            bubble.style.paddingBottom = 10;
            bubble.style.borderTopLeftRadius = 12;
            bubble.style.borderTopRightRadius = 12;
            bubble.style.borderBottomLeftRadius = 12;
            bubble.style.borderBottomRightRadius = 12;

            switch (role)
            {
                case "user":
                    bubble.style.backgroundColor = new Color(0.22f, 0.38f, 0.62f);
                    bubble.style.alignSelf = Align.FlexEnd;
                    bubble.style.maxWidth = Length.Percent(75);
                    bubble.style.borderBottomRightRadius = 4;
                    break;
                case "assistant":
                    bubble.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f);
                    bubble.style.alignSelf = Align.FlexStart;
                    bubble.style.maxWidth = Length.Percent(85);
                    bubble.style.borderBottomLeftRadius = 4;
                    break;
                case "error":
                    bubble.style.backgroundColor = new Color(0.5f, 0.12f, 0.12f);
                    bubble.style.alignSelf = Align.Stretch;
                    break;
            }

            return bubble;
        }

        void AddMessageBubble(string role, string content, int messageIndex = -1)
        {
            if (messageIndex < 0)
                messageIndex = _session != null ? _session.Messages.Count - 1 : -1;

            var wrapper = new VisualElement();
            wrapper.style.position = Position.Relative;
            wrapper.style.alignSelf = role == "user" ? Align.FlexEnd : Align.FlexStart;
            wrapper.style.maxWidth = Length.Percent(role == "user" ? 75 : 90);
            wrapper.style.marginBottom = 10;
            wrapper.userData = messageIndex;

            var bubble = CreateBubble(role);
            bubble.style.marginBottom = 0;
            bubble.style.alignSelf = Align.Stretch;
            bubble.style.maxWidth = Length.Percent(100);

            if (role == "assistant")
            {
                MarkdownRenderer.Render(content, bubble);
            }
            else if (role == "user" && content != null && content.Contains("```"))
            {
                // User message containing code fences — render with markdown so the
                // code block is properly boxed (e.g. Profiler snapshot analysis prompt).
                MarkdownRenderer.Render(content, bubble);
            }
            else
            {
                var label = new Label(content);
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.color = new Color(0.9f, 0.9f, 0.9f);
                label.style.fontSize = 13;
                label.selection.isSelectable = true;
                bubble.Add(label);
            }

            wrapper.Add(bubble);

            if (role != "error")
                AttachHoverCopyButton(wrapper, content, role == "user");

            if (role == "user")
                AttachUserMessageContextMenu(wrapper, messageIndex, content);
            else if (role == "assistant")
                AttachAssistantActions(wrapper, messageIndex, content);

            _messageContainer.Add(wrapper);
            ScrollToBottom();
        }

        void AttachUserMessageContextMenu(VisualElement wrapper, int messageIndex, string content)
        {
            wrapper.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 1) return; // only right-click
                evt.StopPropagation();

                var menu = new GenericMenu();
                if (_session != null && !_session.IsProcessing)
                {
                    menu.AddItem(new GUIContent("Edit and Resend"), false, () => OpenEditDialog(messageIndex, content));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Edit and Resend  (응답 중)"));
                }
                menu.AddItem(new GUIContent("Copy"), false, () => GUIUtility.systemCopyBuffer = content);
                menu.ShowAsContext();
            });
        }

        void AttachAssistantActions(VisualElement wrapper, int messageIndex, string content = null)
        {
            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.marginTop = 4;
            actionRow.style.marginLeft = 4;

            var regenBtn = new VisualElement();
            regenBtn.style.flexDirection = FlexDirection.Row;
            regenBtn.style.alignItems = Align.Center;
            regenBtn.style.height = 22;
            regenBtn.style.paddingLeft = 8;
            regenBtn.style.paddingRight = 8;
            regenBtn.style.backgroundColor = new Color(0.22f, 0.22f, 0.28f);
            regenBtn.style.borderTopLeftRadius = 4;
            regenBtn.style.borderTopRightRadius = 4;
            regenBtn.style.borderBottomLeftRadius = 4;
            regenBtn.style.borderBottomRightRadius = 4;
            regenBtn.tooltip = "이 응답을 새로 생성합니다 (이후 대화는 삭제됨)";

            var regenIcon = VectorIcons.Make(IconType.Refresh, 12);
            regenIcon.style.marginRight = 4;
            regenBtn.Add(regenIcon);

            var regenLabel = new Label("Regenerate");
            regenLabel.style.fontSize = 10;
            regenLabel.style.color = new Color(0.85f, 0.85f, 0.9f);
            regenLabel.pickingMode = PickingMode.Ignore;
            regenBtn.Add(regenLabel);

            regenBtn.AddManipulator(new Clickable(() => RegenerateFromAssistantMessage(messageIndex)));

            actionRow.Add(regenBtn);

            // Per-message token/cost footer (right-aligned).
            if (_session != null && messageIndex >= 0 && messageIndex < _session.Messages.Count)
            {
                var msg = _session.Messages[messageIndex];
                // content match guards against display-only bubbles (e.g. profiler snapshot)
                // picking up an unrelated message's usage.
                if (msg != null && msg.hasUsage && (content == null || msg.content == content))
                {
                    var spacer = new VisualElement();
                    spacer.style.flexGrow = 1;
                    actionRow.Add(spacer);

                    var usageLabel = new Label(FormatUsage(msg));
                    usageLabel.style.fontSize = 10;
                    usageLabel.style.color = new Color(0.55f, 0.55f, 0.6f);
                    usageLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                    usageLabel.selection.isSelectable = true;
                    usageLabel.tooltip = BuildUsageTooltip(msg);
                    actionRow.Add(usageLabel);
                }
            }

            wrapper.Add(actionRow);
        }

        static string FormatUsage(ChatMessage msg)
        {
            int totalIn = msg.inputTokens + msg.cacheCreationTokens + msg.cacheReadTokens;
            var sb = new System.Text.StringBuilder();
            sb.Append($"in {FormatTokenCount(totalIn)} · out {FormatTokenCount(msg.outputTokens)}");
            if (msg.costUsd > 0)
                sb.Append($" · ${msg.costUsd:0.0000}");
            return sb.ToString();
        }

        static string BuildUsageTooltip(ChatMessage msg)
        {
            return
                $"Input: {msg.inputTokens:N0}\n" +
                $"Cache write: {msg.cacheCreationTokens:N0}\n" +
                $"Cache read: {msg.cacheReadTokens:N0}\n" +
                $"Output: {msg.outputTokens:N0}" +
                (msg.costUsd > 0 ? $"\nCost: ${msg.costUsd:0.000000}" : "");
        }

        static string FormatTokenCount(int n)
        {
            if (n >= 1_000_000) return (n / 1_000_000.0).ToString("0.#") + "M";
            if (n >= 1_000) return (n / 1_000.0).ToString("0.#") + "k";
            return n.ToString();
        }

        void OpenEditDialog(int messageIndex, string originalContent)
        {
            if (_session.IsProcessing) return;
            var dialog = EditMessageDialog.Build(originalContent, newContent =>
            {
                ResendFromIndex(messageIndex, newContent);
            });
            rootVisualElement.Add(dialog);
            Repaint();
        }

        void RegenerateFromAssistantMessage(int assistantMessageIndex)
        {
            if (_session == null || _session.IsProcessing) return;
            if (assistantMessageIndex < 1 || assistantMessageIndex >= _session.Messages.Count) return;

            // The user message that produced this assistant response is at index - 1
            int userIdx = assistantMessageIndex - 1;
            if (userIdx < 0 || _session.Messages[userIdx].role != "user")
            {
                // Search backwards for the most recent user message
                userIdx = -1;
                for (int i = assistantMessageIndex - 1; i >= 0; i--)
                {
                    if (_session.Messages[i].role == "user") { userIdx = i; break; }
                }
            }
            if (userIdx < 0) return;

            var userContent = _session.Messages[userIdx].content;
            ResendFromIndex(userIdx, userContent);
        }

        void ResendFromIndex(int truncateIndex, string newContent)
        {
            if (_session == null || _session.IsProcessing) return;
            if (string.IsNullOrWhiteSpace(newContent)) return;

            // Truncate session messages
            _session.TruncateFromIndex(truncateIndex);

            // Rebuild UI bubbles to match
            ReloadMessageBubbles();

            // Show streaming + send
            AddMessageBubble("user", newContent);
            ShowStreamingState();
            _session.SendMessage(newContent);
        }

        static void AttachHoverCopyButton(VisualElement host, string textToCopy, bool isUserMessage)
        {
            var copyBtn = new Button(() =>
            {
                GUIUtility.systemCopyBuffer = textToCopy;
            }) { text = "Copy" };
            copyBtn.style.position = Position.Absolute;
            copyBtn.style.top = -8;
            if (isUserMessage)
                copyBtn.style.left = -8;
            else
                copyBtn.style.right = -8;
            copyBtn.style.width = 24;
            copyBtn.style.height = 24;
            copyBtn.style.paddingLeft = 0;
            copyBtn.style.paddingRight = 0;
            copyBtn.style.fontSize = 10;
            copyBtn.style.backgroundColor = new Color(0.22f, 0.22f, 0.26f);
            copyBtn.style.color = new Color(0.85f, 0.85f, 0.85f);
            copyBtn.style.borderTopLeftRadius = 12;
            copyBtn.style.borderTopRightRadius = 12;
            copyBtn.style.borderBottomLeftRadius = 12;
            copyBtn.style.borderBottomRightRadius = 12;
            copyBtn.style.borderTopWidth = 1;
            copyBtn.style.borderBottomWidth = 1;
            copyBtn.style.borderLeftWidth = 1;
            copyBtn.style.borderRightWidth = 1;
            copyBtn.style.borderTopColor = new Color(0.35f, 0.35f, 0.4f);
            copyBtn.style.borderBottomColor = new Color(0.35f, 0.35f, 0.4f);
            copyBtn.style.borderLeftColor = new Color(0.35f, 0.35f, 0.4f);
            copyBtn.style.borderRightColor = new Color(0.35f, 0.35f, 0.4f);
            copyBtn.style.display = DisplayStyle.None;
            copyBtn.tooltip = "복사";

            copyBtn.clicked += () =>
            {
                copyBtn.text = "OK";
                copyBtn.schedule.Execute(() =>
                {
                    if (copyBtn.text == "OK") copyBtn.text = "Copy";
                }).StartingIn(1200);
            };

            host.RegisterCallback<MouseEnterEvent>(_ => copyBtn.style.display = DisplayStyle.Flex);
            host.RegisterCallback<MouseLeaveEvent>(_ => copyBtn.style.display = DisplayStyle.None);

            host.Add(copyBtn);
        }

        void ScrollToBottom()
        {
            _messageContainer.schedule.Execute(() =>
            {
                _messageContainer.scrollOffset = new Vector2(0, _messageContainer.contentContainer.layout.height);
            });
        }

        void UpdateConnectionStatus()
        {
            UpdateAccountButtonLabel();

            var cliPath = CliLocator.FindClaudeCli();
            if (string.IsNullOrEmpty(cliPath))
            {
                _statusLabel.text = "CLI not found";
                _statusDot.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
                SetDimmed(true);
                return;
            }

            if (!_authManager.IsAuthenticated())
            {
                _statusLabel.text = "Not authenticated";
                _statusDot.style.backgroundColor = new Color(0.9f, 0.6f, 0.1f);
                SetDimmed(true);
                return;
            }

            var mcpInfo = _mcpServer is { IsRunning: true } ? $" | MCP:{_mcpServer.Port}" : " | MCP: failed";
            _statusLabel.text = $"Connected{mcpInfo}";
            _statusDot.style.backgroundColor = _mcpServer is { IsRunning: true }
                ? new Color(0.3f, 0.7f, 0.3f)
                : new Color(0.9f, 0.6f, 0.1f);
            SetDimmed(false);
        }

        void SetDimmed(bool dimmed)
        {
            _inputField.SetEnabled(!dimmed);
            _sendButton.SetEnabled(!dimmed);
            _inputArea.style.opacity = dimmed ? 0.4f : 1f;
        }

        void RestoreSession()
        {
            SessionSerializer.instance.RestoreState(_session);
            for (int i = 0; i < _session.Messages.Count; i++)
                AddMessageBubble(_session.Messages[i].role, _session.Messages[i].content, i);
        }

        void SaveSession()
        {
            HistoryStorage.Save(_session.SessionId, _session.Messages);
            if (_sidebarVisible) RefreshSidebar();
        }
    }
}
