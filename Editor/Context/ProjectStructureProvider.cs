using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ClaudeCode.Editor.Context
{
    public static class ProjectStructureProvider
    {
        public static string GetProjectOverview()
        {
            var sb = new StringBuilder();
            var assetsPath = Application.dataPath;
            var projectPath = Path.GetDirectoryName(assetsPath);

            sb.AppendLine($"[Project] {Path.GetFileName(projectPath)}");
            sb.AppendLine($"  Unity: {Application.unityVersion}");
            sb.AppendLine($"  Platform: {Application.platform}");
            sb.AppendLine($"  Path: {projectPath}");

            return sb.ToString();
        }

        public static string GetAssetsStructure(int maxDepth = 2)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Assets Structure]");
            AppendDirectory(sb, Application.dataPath, 0, maxDepth);
            return sb.ToString();
        }

        public static string GetPackagesInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Packages]");

            var manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (File.Exists(manifestPath))
            {
                var content = File.ReadAllText(manifestPath);
                sb.AppendLine(content);
            }

            return sb.ToString();
        }

        static void AppendDirectory(StringBuilder sb, string path, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;

            var indent = new string(' ', depth * 2 + 2);
            var dirName = Path.GetFileName(path);
            sb.AppendLine($"{indent}{dirName}/");

            var dirs = Directory.GetDirectories(path)
                .Where(d => !Path.GetFileName(d).StartsWith("."))
                .OrderBy(d => d)
                .ToArray();

            foreach (var dir in dirs)
                AppendDirectory(sb, dir, depth + 1, maxDepth);

            if (depth == maxDepth)
            {
                var fileCount = Directory.GetFiles(path).Count(f => !f.EndsWith(".meta"));
                if (fileCount > 0)
                    sb.AppendLine($"{indent}  ({fileCount} files)");
            }
        }
    }
}
