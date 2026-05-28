using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace ClaudeCode.Editor.Core
{
    public enum AuthMethod
    {
        CliLogin,
        ApiKey
    }

    [Serializable]
    public class AccountInfo
    {
        public bool loggedIn;
        public string authMethod;
        public string apiProvider;
        public string email;
        public string orgId;
        public string orgName;
        public string subscriptionType;
    }

    public class AuthManager
    {
        const string ApiKeyPrefKey = "ClaudeCode_ApiKey";
        const string AuthMethodPrefKey = "ClaudeCode_AuthMethod";

        AccountInfo _cachedAccount;
        DateTime _cacheExpiry = DateTime.MinValue;

        public AuthMethod Method
        {
            get => (AuthMethod)EditorPrefs.GetInt(AuthMethodPrefKey, (int)AuthMethod.CliLogin);
            set => EditorPrefs.SetInt(AuthMethodPrefKey, (int)value);
        }

        public string ApiKey
        {
            get => EditorPrefs.GetString(ApiKeyPrefKey, "");
            set => EditorPrefs.SetString(ApiKeyPrefKey, value);
        }

        public bool IsAuthenticated()
        {
            return Method switch
            {
                AuthMethod.ApiKey => !string.IsNullOrEmpty(ApiKey),
                AuthMethod.CliLogin => GetAccountInfo()?.loggedIn == true,
                _ => false
            };
        }

        public AccountInfo GetAccountInfo(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedAccount != null && DateTime.Now < _cacheExpiry)
                return _cachedAccount;

            _cachedAccount = FetchAccountInfo();
            _cacheExpiry = DateTime.Now.AddSeconds(30);
            return _cachedAccount;
        }

        public ProcessStartInfo ApplyAuth(ProcessStartInfo startInfo)
        {
            if (Method == AuthMethod.ApiKey && !string.IsNullOrEmpty(ApiKey))
                startInfo.EnvironmentVariables["ANTHROPIC_API_KEY"] = ApiKey;
            return startInfo;
        }

        static AccountInfo FetchAccountInfo()
        {
            try
            {
                var cliPath = CliLocator.FindClaudeCli();
                if (string.IsNullOrEmpty(cliPath)) return null;

                var psi = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = "auth status --json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return null;

                var json = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(5000);

                if (proc.ExitCode != 0 || string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonUtility.FromJson<AccountInfo>(json);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[ClaudeCode] Account info fetch failed: {e.Message}");
                return null;
            }
        }

        public void InvalidateCache()
        {
            _cachedAccount = null;
            _cacheExpiry = DateTime.MinValue;
        }

        public static void OpenLoginTerminal()
        {
            try
            {
                var cliPath = CliLocator.FindClaudeCli();
                if (string.IsNullOrEmpty(cliPath))
                {
                    EditorUtility.DisplayDialog("Claude Code", "CLI not found. Install it first.", "OK");
                    return;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    LaunchWindowsTerminal(cliPath);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = $"-e 'tell application \"Terminal\" to do script \"\\\"{cliPath}\\\" auth login\"'",
                        UseShellExecute = false
                    });
                else
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xterm",
                        Arguments = $"-e {cliPath} auth login",
                        UseShellExecute = false
                    });
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Claude Code", $"Failed to open terminal: {e.Message}", "OK");
            }
        }

        static void LaunchWindowsTerminal(string cliPath)
        {
            var psCommand = $"& '{cliPath}' auth login";

            if (TryStart("wt.exe", $"powershell -NoExit -Command \"{psCommand}\"")) return;
            if (TryStart("powershell.exe", $"-NoExit -Command \"{psCommand}\"")) return;

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k \"\"{cliPath}\" auth login\"",
                UseShellExecute = true
            });
        }

        static bool TryStart(string fileName, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = true
                };
                var p = Process.Start(psi);
                return p != null;
            }
            catch
            {
                return false;
            }
        }

        public void Logout()
        {
            try
            {
                var cliPath = CliLocator.FindClaudeCli();
                if (string.IsNullOrEmpty(cliPath)) return;

                var psi = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = "auth logout",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                proc?.WaitForExit(5000);
                InvalidateCache();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[ClaudeCode] Logout failed: {e.Message}");
            }
        }
    }
}
