using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UpmPackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageSource = UnityEditor.PackageManager.PackageSource;

namespace ClaudeCode.Editor.Core
{
    /// <summary>
    /// Checks GitHub (public repo) for a newer package version by fetching the raw
    /// package.json on main. Runs once per editor session: SessionState survives
    /// domain reloads (so script recompiles don't re-fetch) but resets when the
    /// editor restarts — i.e. every Unity launch gets exactly one check.
    /// Results are exposed as polled state — the ChatWindow shows a badge when an
    /// update is available.
    ///
    /// Skipped entirely when the package is embedded/local (we ARE the source then).
    /// </summary>
    [InitializeOnLoad]
    public static class UpdateChecker
    {
        const string RawPackageJsonUrl =
            "https://raw.githubusercontent.com/YeongJunSeong/claude-code-unitypackage/main/package.json";

        const string SessionCheckedKey = "ClaudeCode_UpdateCheck_DoneThisSession";
        const string CachedRemotePrefKey = "ClaudeCode_UpdateCheck_RemoteVersion";

        static readonly Regex VersionRegex =
            new Regex("\"version\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);

        public static string InstalledVersion { get; private set; }
        public static string RemoteVersion { get; private set; }

        static System.Threading.SynchronizationContext _mainCtx;

        /// <summary>True when the remote version is strictly newer than the installed one.</summary>
        public static bool HasUpdate
        {
            get
            {
                if (string.IsNullOrEmpty(InstalledVersion) || string.IsNullOrEmpty(RemoteVersion))
                    return false;
                return IsNewer(RemoteVersion, InstalledVersion);
            }
        }

        static UpdateChecker()
        {
            try { Init(); } catch { }
        }

        static void Init()
        {
            // InitializeOnLoad runs on the main thread — capture its context for
            // marshaling network results back from the ThreadPool.
            _mainCtx = System.Threading.SynchronizationContext.Current;

            var info = UpmPackageInfo.FindForAssembly(typeof(UpdateChecker).Assembly);
            if (info == null) return;

            InstalledVersion = info.version;

            // Developing the package locally — never nag ourselves.
            if (info.source == PackageSource.Embedded || info.source == PackageSource.Local)
                return;

            // Show cached result immediately (badge appears without waiting for network).
            RemoteVersion = EditorPrefs.GetString(CachedRemotePrefKey, null);

            // Once per editor session: survives domain reloads, resets on editor restart.
            if (SessionState.GetBool(SessionCheckedKey, false)) return;
            SessionState.SetBool(SessionCheckedKey, true);

            _ = FetchRemoteVersionAsync();
        }

        static async Task FetchRemoteVersionAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                var json = await client.GetStringAsync(RawPackageJsonUrl).ConfigureAwait(false);
                var m = VersionRegex.Match(json ?? "");
                if (!m.Success) return;
                var remote = m.Groups[1].Value;

                // Back to main thread for EditorPrefs + state.
                if (_mainCtx != null)
                {
                    _mainCtx.Post(_ =>
                    {
                        RemoteVersion = remote;
                        EditorPrefs.SetString(CachedRemotePrefKey, remote);
                    }, null);
                }
                else
                {
                    RemoteVersion = remote; // string write is safe; skip prefs caching
                }
            }
            catch
            {
                // Offline / GitHub unreachable — silently skip; try again after the throttle window.
            }
        }

        static bool IsNewer(string remote, string installed)
        {
            if (Version.TryParse(remote, out var r) && Version.TryParse(installed, out var i))
                return r > i;
            return false;
        }

        /// <summary>Opens the Package Manager window with this package selected.</summary>
        public static void OpenPackageManager()
        {
            UnityEditor.PackageManager.UI.Window.Open("com.dnsoft.claudecode");
        }

        /// <summary>Bypasses the daily throttle and checks GitHub immediately.</summary>
        [MenuItem("Tools/Claude Code/Check for Updates", false, 30)]
        public static void ForceCheck()
        {
            if (_mainCtx == null)
                _mainCtx = System.Threading.SynchronizationContext.Current;

            var info = UpmPackageInfo.FindForAssembly(typeof(UpdateChecker).Assembly);
            if (info == null) return;
            InstalledVersion = info.version;

            if (info.source == PackageSource.Embedded || info.source == PackageSource.Local)
            {
                EditorUtility.DisplayDialog("Claude Code",
                    "패키지를 개발 중인 프로젝트(embedded)에서는 업데이트 확인이 비활성화됩니다.", "OK");
                return;
            }

            SessionState.SetBool(SessionCheckedKey, true);
            _ = FetchRemoteVersionAsync().ContinueWith(_2 =>
            {
                _mainCtx?.Post(_3 =>
                {
                    if (HasUpdate)
                        EditorUtility.DisplayDialog("Claude Code",
                            $"새 버전 v{RemoteVersion} 사용 가능 (현재 v{InstalledVersion}).\n" +
                            "채팅창 상단의 NEW 뱃지를 클릭하거나 Package Manager에서 업데이트하세요.", "OK");
                    else
                        EditorUtility.DisplayDialog("Claude Code",
                            $"최신 버전입니다 (v{InstalledVersion}).", "OK");
                }, null);
            });
        }
    }
}
