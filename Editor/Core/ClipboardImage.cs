using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace ClaudeCode.Editor.Core
{
    public static class ClipboardImage
    {
        public static string TryPasteImage()
        {
#if UNITY_EDITOR_WIN
            try
            {
                var dir = GetAttachmentsDir();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var fileName = $"paste_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                var fullPath = Path.Combine(dir, fileName).Replace("\\", "/");

                var psScript =
                    "$ErrorActionPreference='Stop'; " +
                    "Add-Type -AssemblyName System.Windows.Forms; " +
                    "Add-Type -AssemblyName System.Drawing; " +
                    "$img = Get-Clipboard -Format Image -ErrorAction SilentlyContinue; " +
                    "if ($img -eq $null) { $img = [System.Windows.Forms.Clipboard]::GetImage() }; " +
                    "if ($img -ne $null) { " +
                    $"  $img.Save('{fullPath}', [System.Drawing.Imaging.ImageFormat]::Png); " +
                    "  Write-Output 'OK' " +
                    "} else { Write-Output 'NO_IMAGE' }";

                var psExe = ResolvePowerShellPath();
                if (string.IsNullOrEmpty(psExe))
                {
                    UnityEngine.Debug.LogError("[ClaudeCode] PowerShell executable not found");
                    return null;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = psExe,
                    Arguments = $"-Sta -NoProfile -Command \"{psScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    UnityEngine.Debug.LogError($"[ClaudeCode] Process.Start returned null for {psExe}");
                    return null;
                }

                var stdout = proc.StandardOutput.ReadToEnd().Trim();
                var stderr = proc.StandardError.ReadToEnd().Trim();
                proc.WaitForExit(5000);

                if (!string.IsNullOrEmpty(stderr))
                    UnityEngine.Debug.LogWarning($"[ClaudeCode] PS stderr: {stderr}");

                if (stdout.Contains("OK") && File.Exists(fullPath))
                    return MakeRelativePath(fullPath);

                return null;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ClaudeCode] Clipboard image paste EXCEPTION: {e}");
                return null;
            }
#else
            return null;
#endif
        }

#if UNITY_EDITOR_WIN
        static string ResolvePowerShellPath()
        {
            var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var candidates = new[]
            {
                Path.Combine(systemDir, "WindowsPowerShell", "v1.0", "powershell.exe"),
                @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                @"C:\Program Files\PowerShell\7\pwsh.exe",
                @"C:\Program Files (x86)\PowerShell\7\pwsh.exe",
            };

            foreach (var c in candidates)
            {
                if (!string.IsNullOrEmpty(c) && File.Exists(c)) return c;
            }
            return null;
        }
#endif

        public static string GetAttachmentsDir()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, "Library", "ClaudeCodeAttachments");
        }

        static string MakeRelativePath(string fullPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace("\\", "/");
            var normalized = fullPath.Replace("\\", "/");
            if (!string.IsNullOrEmpty(projectRoot) && normalized.StartsWith(projectRoot))
                return normalized.Substring(projectRoot.Length).TrimStart('/');
            return normalized;
        }
    }
}
