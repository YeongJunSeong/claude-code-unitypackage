using UnityEditor;
using UnityEngine;

namespace ClaudeCode.Editor.UI
{
    public static class HierarchyContextMenu
    {
        const string Root = "GameObject/Ask Claude/";

        [MenuItem(Root + "Explain this GameObject", false, 0)]
        static void Explain(MenuCommand command) => Run(command, "이 GameObject가 무엇이고 어떤 역할을 하는지, 붙어있는 컴포넌트들과 함께 설명해줘.");
        [MenuItem(Root + "Explain this GameObject", true)]
        static bool ValidateExplain(MenuCommand command) => GetGameObject(command) != null;

        [MenuItem(Root + "Suggest improvements", false, 1)]
        static void Improve(MenuCommand command) => Run(command, "이 GameObject의 구조/컴포넌트 구성에 대해 개선 방안을 제안해줘.");
        [MenuItem(Root + "Suggest improvements", true)]
        static bool ValidateImprove(MenuCommand command) => GetGameObject(command) != null;

        [MenuItem(Root + "Find issues", false, 2)]
        static void FindIssues(MenuCommand command) => Run(command, "이 GameObject에 잠재적인 문제나 흔한 실수가 있는지 점검해줘.");
        [MenuItem(Root + "Find issues", true)]
        static bool ValidateFindIssues(MenuCommand command) => GetGameObject(command) != null;

        [MenuItem(Root + "Optimize performance", false, 3)]
        static void Optimize(MenuCommand command) => Run(command, "이 GameObject의 성능 최적화 방안을 알려줘.");
        [MenuItem(Root + "Optimize performance", true)]
        static bool ValidateOptimize(MenuCommand command) => GetGameObject(command) != null;

        [MenuItem(Root + "Add to chat", false, 20)]
        static void AddToChat(MenuCommand command)
        {
            var go = GetGameObject(command);
            if (go == null) return;
            var window = ChatWindow.ShowAndFocus();
            window.AttachGameObjectOnly(go);
        }
        [MenuItem(Root + "Add to chat", true)]
        static bool ValidateAddToChat(MenuCommand command) => GetGameObject(command) != null;

        static void Run(MenuCommand command, string prompt)
        {
            var go = GetGameObject(command);
            if (go == null) return;
            var window = ChatWindow.ShowAndFocus();
            window.StartTaskWithGameObject(go, prompt);
        }

        static GameObject GetGameObject(MenuCommand command)
        {
            return command.context as GameObject ?? Selection.activeGameObject;
        }
    }
}
