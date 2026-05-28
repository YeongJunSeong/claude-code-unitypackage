using System;
using System.Collections.Generic;

namespace ClaudeCode.Editor.UI
{
    public class SlashCommand
    {
        public string Name;
        public string Description;
        public string Icon;
        public bool TakesArguments;
        public Action<ChatWindow, string> Execute;
    }

    public static class SlashCommandRegistry
    {
        public static readonly List<SlashCommand> All = new List<SlashCommand>
        {
            new SlashCommand
            {
                Name = "/clear",
                Description = "현재 대화 초기화",
                Icon = "•",
                Execute = (w, _) => w.ExecuteClearCommand()
            },
            new SlashCommand
            {
                Name = "/new",
                Description = "새 세션 시작",
                Icon = "+",
                Execute = (w, _) => w.ExecuteClearCommand()
            },
            new SlashCommand
            {
                Name = "/sessions",
                Description = "히스토리 사이드바 토글",
                Icon = "≡",
                Execute = (w, _) => w.ExecuteToggleSidebarCommand()
            },
            new SlashCommand
            {
                Name = "/login",
                Description = "Claude 계정 로그인",
                Icon = "→",
                Execute = (w, _) => w.ExecuteLoginCommand()
            },
            new SlashCommand
            {
                Name = "/logout",
                Description = "로그아웃",
                Icon = "←",
                Execute = (w, _) => w.ExecuteLogoutCommand()
            },
            new SlashCommand
            {
                Name = "/copy",
                Description = "마지막 응답을 클립보드에 복사",
                Icon = "⎘",
                Execute = (w, _) => w.ExecuteCopyLastCommand()
            },
            new SlashCommand
            {
                Name = "/model",
                Description = "모델 변경 (예: /model opus)",
                Icon = "*",
                TakesArguments = true,
                Execute = (w, args) => w.ExecuteModelCommand(args)
            },
            new SlashCommand
            {
                Name = "/settings",
                Description = "프로젝트 설정 열기",
                Icon = "≡",
                Execute = (w, _) => w.ExecuteSettingsCommand()
            },
            new SlashCommand
            {
                Name = "/help",
                Description = "사용 가능한 명령어 보기",
                Icon = "?",
                Execute = (w, _) => w.ExecuteHelpCommand()
            },
        };

        public static List<SlashCommand> Filter(string query)
        {
            if (string.IsNullOrEmpty(query) || query == "/") return All;
            var lower = query.ToLowerInvariant();
            var result = new List<SlashCommand>();
            foreach (var c in All)
                if (c.Name.ToLowerInvariant().StartsWith(lower))
                    result.Add(c);
            return result;
        }

        public static SlashCommand FindExact(string name)
        {
            foreach (var c in All)
                if (c.Name == name) return c;
            return null;
        }
    }
}
