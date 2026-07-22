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
    /// package.json on main. Runs at most once per day (throttled via EditorPrefs),
    /// on editor load. Results are exposed as polled state — the ChatWindow shows a
    /// badge when an update is available.
    ///
    /// Skipped entirely when the package is embedded/local (we ARE the source then).
    /// </summary>
    [InitializeOnLoad]
    public static class UpdateChecker
    {
        const string RawPackageJsonUrl =
            "https://raw.githubusercontent.com/YeongJunSeong/claude-code-unitypackage/main/package.json";

        const string LastCheckPrefKey = "ClaudeCode_UpdateCheck_LastTicks";
        const string CachedRemotePrefKey = "ClaudeCode_UpdateCheck_RemoteVersion";
        const double CheckIntervalHours = 24;

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

            // Throttle: at most one network check per day.
            var lastTicksStr = EditorPrefs.GetString(LastCheckPrefKey, "0");
            long.TryParse(lastTicksStr, out var lastTicks);
            var elapsed = DateTime.UtcNow - new DateTime(lastTicks, DateTimeKind.Utc);
            if (elapsed.TotalHours < CheckIntervalHours) return;

            EditorPrefs.SetString(LastCheckPrefKey, DateTime.UtcNow.Ticks.ToString());
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
    }
}
