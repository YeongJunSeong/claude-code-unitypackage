using System.Text;

namespace ClaudeCode.Editor.Context
{
    public static class ContextCollector
    {
        public static string CollectAll()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Unity Project Context ===");
            sb.AppendLine();
            sb.AppendLine(ProjectStructureProvider.GetProjectOverview());
            sb.AppendLine(SceneContextProvider.GetOpenScenesInfo());
            sb.AppendLine(SceneContextProvider.GetActiveSceneInfo());
            sb.AppendLine(SceneContextProvider.GetSelectedObjectsInfo());
            sb.AppendLine(ConsoleLogProvider.GetRecentErrors());
            return sb.ToString();
        }

        public static string CollectMinimal()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Unity Context ===");
            sb.AppendLine(ProjectStructureProvider.GetProjectOverview());
            sb.AppendLine(SceneContextProvider.GetActiveSceneInfo());
            sb.AppendLine(SceneContextProvider.GetSelectedObjectsInfo());
            return sb.ToString();
        }
    }
}
