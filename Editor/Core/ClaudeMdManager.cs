using System.IO;
using UnityEngine;

namespace ClaudeCode.Editor.Core
{
    public static class ClaudeMdManager
    {
        public const string FileName = "CLAUDE.md";

        public static string GetProjectRoot()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }

        public static string GetProjectClaudeMdPath()
        {
            var root = GetProjectRoot();
            return string.IsNullOrEmpty(root) ? FileName : Path.Combine(root, FileName);
        }

        public static bool Exists()
        {
            return File.Exists(GetProjectClaudeMdPath());
        }

        public static string Read()
        {
            var path = GetProjectClaudeMdPath();
            if (!File.Exists(path)) return null;
            try { return File.ReadAllText(path); }
            catch { return null; }
        }

        public class PathValidationResult
        {
            public bool IsValid;
            public string NormalizedPath;
            public string ErrorMessage;
        }

        public static PathValidationResult ValidatePath(string inputPath)
        {
            var result = new PathValidationResult();

            if (string.IsNullOrWhiteSpace(inputPath))
            {
                result.ErrorMessage = "경로가 비어 있습니다.";
                return result;
            }

            string fullPath;
            try
            {
                fullPath = Path.IsPathRooted(inputPath)
                    ? Path.GetFullPath(inputPath)
                    : Path.GetFullPath(Path.Combine(GetProjectRoot(), inputPath));
            }
            catch (System.Exception e)
            {
                result.ErrorMessage = $"경로 형식이 올바르지 않습니다: {e.Message}";
                return result;
            }

            // 파일명 검증
            var fileName = Path.GetFileName(fullPath);
            if (!fileName.Equals(FileName, System.StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = $"파일명은 반드시 '{FileName}'이어야 합니다. (현재: '{fileName}')";
                return result;
            }

            // 프로젝트 외부 경로 차단
            var projectRoot = Path.GetFullPath(GetProjectRoot());
            var normalizedProjectRoot = projectRoot.Replace("\\", "/").TrimEnd('/');
            var normalizedFullPath = fullPath.Replace("\\", "/");
            if (!normalizedFullPath.StartsWith(normalizedProjectRoot + "/", System.StringComparison.OrdinalIgnoreCase) &&
                !normalizedFullPath.Equals(normalizedProjectRoot + "/" + FileName, System.StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = $"Unity 프로젝트 외부의 경로에는 생성할 수 없습니다.\n프로젝트 루트: {normalizedProjectRoot}";
                return result;
            }

            // 상위 디렉토리 존재 여부
            var parentDir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                result.ErrorMessage = $"상위 디렉토리가 존재하지 않습니다:\n{parentDir}";
                return result;
            }

            // 이미 존재하는 파일이 있는 경우 알림 (덮어쓰기 경고)
            if (File.Exists(fullPath))
            {
                result.IsValid = true;
                result.NormalizedPath = fullPath;
                result.ErrorMessage = "! 이 경로에 이미 CLAUDE.md 파일이 존재합니다. 진행 시 Claude가 해당 파일을 업데이트합니다.";
                return result;
            }

            result.IsValid = true;
            result.NormalizedPath = fullPath;
            return result;
        }
    }
}
