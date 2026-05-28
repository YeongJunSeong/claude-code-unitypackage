using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClaudeCode.Editor.Context
{
    public static class SceneContextProvider
    {
        public static string GetActiveSceneInfo()
        {
            var scene = SceneManager.GetActiveScene();
            var sb = new StringBuilder();
            sb.AppendLine($"[Active Scene] {scene.name} ({scene.path})");
            sb.AppendLine($"  Root objects: {scene.rootCount}");
            sb.AppendLine($"  Dirty: {scene.isDirty}");

            foreach (var root in scene.GetRootGameObjects())
                AppendHierarchy(sb, root.transform, 1, 3);

            return sb.ToString();
        }

        public static string GetSelectedObjectsInfo()
        {
            var selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
                return "[Selection] None";

            var sb = new StringBuilder();
            sb.AppendLine($"[Selection] {selection.Length} object(s)");

            foreach (var go in selection)
            {
                sb.AppendLine($"  - {go.name} (active={go.activeSelf})");
                sb.AppendLine($"    Path: {GetGameObjectPath(go)}");

                var components = go.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    sb.AppendLine($"    Component: {comp.GetType().Name}");
                }
            }

            return sb.ToString();
        }

        public static string GetOpenScenesInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Open Scenes]");

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                sb.AppendLine($"  - {scene.name} (loaded={scene.isLoaded}, path={scene.path})");
            }

            return sb.ToString();
        }

        static void AppendHierarchy(StringBuilder sb, Transform t, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;

            var indent = new string(' ', depth * 2 + 2);
            sb.AppendLine($"{indent}- {t.name}");

            for (int i = 0; i < t.childCount && i < 20; i++)
                AppendHierarchy(sb, t.GetChild(i), depth + 1, maxDepth);

            if (t.childCount > 20)
                sb.AppendLine($"{indent}  ... and {t.childCount - 20} more");
        }

        static string GetGameObjectPath(GameObject go)
        {
            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
