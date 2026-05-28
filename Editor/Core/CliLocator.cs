using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ClaudeCode.Editor.Core
{
    public static class CliLocator
    {
        public static string FindClaudeCli()
        {
            var envPath = Environment.GetEnvironmentVariable("CLAUDE_CLI_PATH");
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                return envPath;

            var whichResult = RunWhich();
            if (!string.IsNullOrEmpty(whichResult))
                return whichResult;

            foreach (var candidate in GetDefaultPaths())
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        static string RunWhich()
        {
            try
            {
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                var psi = new ProcessStartInfo
                {
                    FileName = isWindows ? "where" : "which",
                    Arguments = "claude",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return null;

                var output = proc.StandardOutput.ReadLine();
                proc.WaitForExit(3000);
                return proc.ExitCode == 0 ? output?.Trim() : null;
            }
            catch
            {
                return null;
            }
        }

        static string[] GetDefaultPaths()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
