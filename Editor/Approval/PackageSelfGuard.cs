using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UpmPackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageSource = UnityEditor.PackageManager.PackageSource;

namespace ClaudeCode.Editor.Approval
{
    /// <summary>
    /// Prevents Claude from modifying the Claude Code package's own files when the
    /// package is installed as an immutable UPM package (Git / Registry / tarball,
    /// i.e. living under Library/PackageCache).
    ///
    /// When the package is embedded/local (i.e. we are actively developing it in this
    /// project), self-modification is allowed so development isn't blocked.
    /// </summary>
    [InitializeOnLoad]
    public static class PackageSelfGuard
    {
        const string PackageName = "com.dnsoft.claudecode";

        static bool _resolved;
        static bool _isImmutable;
        static string _packageRootNormalized;

        // Resolve eagerly on the main thread at editor load. PackageInfo queries are
        // safest on the main thread, and PermissionPromptTool runs on a background thread.
        static PackageSelfGuard()
        {
            try { Resolve(); } catch { }
        }

        static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            try
            {
                var info = UpmPackageInfo.FindForAssembly(typeof(PackageSelfGuard).Assembly);
                if (info == null || info.name != PackageName)
                {
                    // Could not resolve our own package info — be safe and do NOT block,
                    // since blocking on a misdetection would break the dev project.
                    _isImmutable = false;
                    _packageRootNormalized = null;
                    return;
                }

                // Immutable sources: Git, Registry, LocalTarball, BuiltIn.
                // Mutable (dev) sources: Embedded, Local.
                switch (info.source)
                {
                    case PackageSource.Git:
                    case PackageSource.Registry:
                    case PackageSource.LocalTarball:
                    case PackageSource.BuiltIn:
                        _isImmutable = true;
                        break;
                    default: // Embedded, Local, Unknown
                        _isImmutable = false;
                        break;
                }

                _packageRootNormalized = Normalize(info.resolvedPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ClaudeCode] PackageSelfGuard resolve failed (allowing edits): {e.Message}");
                _isImmutable = false;
                _packageRootNormalized = null;
            }
        }

        /// <summary>True when this package is installed as a read-only UPM package.</summary>
        public static bool IsImmutable
        {
            get { Resolve(); return _isImmutable; }
        }

        /// <summary>
        /// Returns true if a write to the given tool input should be blocked because it
        /// targets this package's own files while the package is immutable.
        /// </summary>
        public static bool ShouldBlock(string toolName, Dictionary<string, object> input, out string blockedPath)
        {
            blockedPath = null;
            Resolve();

            if (!_isImmutable || string.IsNullOrEmpty(_packageRootNormalized)) return false;
            if (input == null) return false;

            // 1) 파일 편집 도구: 대상 경로가 패키지 내부면 차단.
            if (IsFileWritingTool(toolName))
            {
                foreach (var path in ExtractTargetPaths(toolName, input))
                {
                    var norm = Normalize(path);
                    if (string.IsNullOrEmpty(norm)) continue;
                    if (IsInside(norm, _packageRootNormalized))
                    {
                        blockedPath = path;
                        return true;
                    }
                }
                return false;
            }

            // 2) Bash/셸 도구: 명령 문자열이 패키지 경로나 패키지 ID를 참조하면 차단.
            //    (cp/mv/rm/리다이렉트/sed -i 등으로 패키지를 복사·이동·삭제해서
            //     파일 편집 가드를 우회하려는 시도를 막는다.)
            if (IsShellTool(toolName))
            {
                if (input.TryGetValue("command", out var cmdObj) && cmdObj != null)
                {
                    var raw = cmdObj.ToString();
                    var normCmd = raw.Replace('\\', '/');
                    if (Application.platform == RuntimePlatform.WindowsEditor)
                        normCmd = normCmd.ToLowerInvariant();

                    if (normCmd.Contains(PackageName) ||
                        normCmd.Contains(_packageRootNormalized))
                    {
                        blockedPath = "(shell) " + raw;
                        return true;
                    }
                }
                return false;
            }

            return false;
        }

        static bool IsShellTool(string toolName)
        {
            if (string.IsNullOrEmpty(toolName)) return false;
            switch (toolName)
            {
                case "Bash":
                case "Shell":
                    return true;
                default:
                    return false;
            }
        }

        static IEnumerable<string> ExtractTargetPaths(string toolName, Dictionary<string, object> input)
        {
            // Write / Edit / MultiEdit use "file_path"; NotebookEdit uses "notebook_path".
            if (input.TryGetValue("file_path", out var fp) && fp != null)
                yield return fp.ToString();
            if (input.TryGetValue("notebook_path", out var np) && np != null)
                yield return np.ToString();
        }

        static bool IsFileWritingTool(string toolName)
        {
            if (string.IsNullOrEmpty(toolName)) return false;
            switch (toolName)
            {
                case "Write":
                case "Edit":
                case "MultiEdit":
                case "NotebookEdit":
                    return true;
                default:
                    return false;
            }
        }

        static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                var full = Path.GetFullPath(path);
                full = full.Replace('\\', '/').TrimEnd('/');
                // Windows file system is case-insensitive.
                return Application.platform == RuntimePlatform.WindowsEditor
                    ? full.ToLowerInvariant()
                    : full;
            }
            catch
            {
                return null;
            }
        }

        static bool IsInside(string candidate, string root)
        {
            if (candidate == root) return true;
            return candidate.StartsWith(root + "/", StringComparison.Ordinal);
        }
    }
}
