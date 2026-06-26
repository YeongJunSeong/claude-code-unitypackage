using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ClaudeCode.Editor.Core
{
    public static class CliLocator
    {
        static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static string FindClaudeCli()
        {
            var envPath = Environment.GetEnvironmentVariable("CLAUDE_CLI_PATH");
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                return NormalizeWindows(envPath);

            var whichResult = RunWhich();
            if (!string.IsNullOrEmpty(whichResult))
                return NormalizeWindows(whichResult);

            foreach (var candidate in GetDefaultPaths())
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Configures a ProcessStartInfo to launch the Claude CLI correctly.
        /// On Windows, .cmd/.bat shims cannot be executed directly with
        /// UseShellExecute=false (they raise "%1 is not a valid Win32 application"),
        /// so they must be run through cmd.exe.
        /// </summary>
        public static void ConfigureStartInfo(ProcessStartInfo psi, string cliPath, string arguments)
        {
            var ext = Path.GetExtension(cliPath)?.ToLowerInvariant();
            if (IsWindows && (ext == ".cmd" || ext == ".bat"))
            {
                var comspec = Environment.GetEnvironmentVariable("ComSpec");
                if (string.IsNullOrEmpty(comspec)) comspec = "cmd.exe";
                psi.FileName = comspec;
                // /d: skip AutoRun, /s + outer quotes: parse the quoted command correctly,
                // /c: run then terminate.
                psi.Arguments = $"/d /s /c \"\"{cliPath}\" {arguments}\"";
            }
            else
            {
                psi.FileName = cliPath;
                psi.Arguments = arguments;
            }
        }

        static string RunWhich()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = IsWindows ? "where" : "which",
                    Arguments = "claude",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return null;

                var lines = new List<string>();
                string line;
                while ((line = proc.StandardOutput.ReadLine()) != null)
                {
                    var t = line.Trim();
                    if (!string.IsNullOrEmpty(t)) lines.Add(t);
                }
                proc.WaitForExit(3000);
                if (proc.ExitCode != 0 || lines.Count == 0) return null;

                return IsWindows ? PickBestWindows(lines) : lines[0];
            }
            catch
            {
                return null;
            }
        }

        // Prefer a real executable over the extensionless npm bash shim:
        // .exe > .cmd > .bat > anything else.
        static string PickBestWindows(List<string> paths)
        {
            string Find(string ext)
            {
                foreach (var p in paths)
                    if (p.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return p;
                return null;
            }
            return Find(".exe") ?? Find(".cmd") ?? Find(".bat") ?? paths[0];
        }

        // If a Windows path has no runnable extension (npm bash shim), switch to the
        // .cmd / .exe sibling that npm/installers create alongside it.
        static string NormalizeWindows(string path)
        {
            if (!IsWindows || string.IsNullOrEmpty(path)) return path;

            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".exe" || ext == ".cmd" || ext == ".bat") return path;

            if (File.Exists(path + ".cmd")) return path + ".cmd";
            if (File.Exists(path + ".exe")) return path + ".exe";
            return path;
        }

        static string[] GetDefaultPaths()
        {
            if (IsWindows)
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return new[]
                {
                    Path.Combine(localAppData, "Programs", "claude-code", "claude.exe"),
                    Path.Combine(appData, "npm", "claude.cmd"),
                    Path.Combine(appData, "npm", "claude"),
                };
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new[]
            {
                "/usr/local/bin/claude",
                "/usr/bin/claude",
                Path.Combine(home, ".npm-global", "bin", "claude"),
                Path.Combine(home, ".local", "bin", "claude"),
            };
        }
    }
}
