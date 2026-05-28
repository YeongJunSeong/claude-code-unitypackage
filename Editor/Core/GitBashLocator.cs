using System;
using System.IO;

namespace ClaudeCode.Editor.Core
{
    public static class GitBashLocator
    {
        public static string FindGitBash()
        {
            var envPath = Environment.GetEnvironmentVariable("GIT_BASH_PATH");
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                return envPath;

            foreach (var p in GetCandidates())
            {
                if (File.Exists(p)) return p;
            }
            return null;
        }

        static string[] GetCandidates()
        {
            var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return new[]
            {
                Path.Combine(pf, "Git", "git-bash.exe"),
                Path.Combine(pf86, "Git", "git-bash.exe"),
                Path.Combine(localApp, "Programs", "Git", "git-bash.exe"),
                @"C:\Program Files\Git\git-bash.exe",
                @"C:\Program Files (x86)\Git\git-bash.exe"
            };
        }
    }
}
