namespace ClaudeCode.Editor.UI
{
    /// <summary>
    /// Central dispatcher for code block syntax highlighting.
    /// Returns Unity rich-text-tagged string for supported languages,
    /// or null when no highlighter handles the language.
    /// </summary>
    public static class CodeHighlighter
    {
        public static string Highlight(string code, string language)
        {
            if (string.IsNullOrEmpty(code)) return null;

            if (CSharpHighlighter.IsCSharpLanguage(language)) return CSharpHighlighter.Highlight(code);
            if (JsonHighlighter.IsJsonLanguage(language))     return JsonHighlighter.Highlight(code);
            if (YamlHighlighter.IsYamlLanguage(language))     return YamlHighlighter.Highlight(code);
            if (ShellHighlighter.IsShellLanguage(language))   return ShellHighlighter.Highlight(code);
            if (ShaderHighlighter.IsShaderLanguage(language)) return ShaderHighlighter.Highlight(code);

            return null;
        }

        public static bool IsSupported(string language)
        {
            return CSharpHighlighter.IsCSharpLanguage(language)
                || JsonHighlighter.IsJsonLanguage(language)
                || YamlHighlighter.IsYamlLanguage(language)
                || ShellHighlighter.IsShellLanguage(language)
                || ShaderHighlighter.IsShaderLanguage(language);
        }
    }
}
