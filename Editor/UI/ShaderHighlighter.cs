using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeCode.Editor.UI
{
    public static class ShaderHighlighter
    {
        // ShaderLab + HLSL/Cg keywords
        static readonly HashSet<string> Keywords = new HashSet<string>
        {
            // ShaderLab
            "Shader", "SubShader", "Pass", "Properties", "Tags", "LOD", "Cull", "ZWrite",
            "ZTest", "Blend", "BlendOp", "ColorMask", "Stencil", "Offset", "Fog", "Lighting",
            "Material", "SetTexture", "GrabPass", "UsePass", "Fallback", "CustomEditor",
            "CGPROGRAM", "ENDCG", "HLSLPROGRAM", "ENDHLSL", "HLSLINCLUDE", "ENDHLSL",
            "CGINCLUDE", "ENDCG", "Category", "Name",

            // HLSL/Cg keywords
            "struct", "return", "void", "if", "else", "for", "while", "do", "break", "continue",
            "switch", "case", "default", "inline", "static", "const", "uniform", "in", "out",
            "inout", "true", "false", "discard", "typedef", "register", "namespace"
        };

        static readonly HashSet<string> Types = new HashSet<string>
        {
            // HLSL primitive types
            "float", "float2", "float3", "float4", "float2x2", "float3x3", "float4x4", "float3x4", "float4x3",
            "half", "half2", "half3", "half4", "half4x4", "half3x3",
            "fixed", "fixed2", "fixed3", "fixed4",
            "double", "double2", "double3", "double4",
            "int", "int2", "int3", "int4",
            "uint", "uint2", "uint3", "uint4",
            "bool", "bool2", "bool3", "bool4",
            "min16float", "min16int", "min16uint",

            // Sampler/Texture types
            "sampler2D", "sampler3D", "samplerCUBE", "sampler2D_half", "sampler2D_float",
            "Texture2D", "Texture3D", "TextureCube", "Texture2DArray", "TextureCubeArray",
            "SamplerState", "SamplerComparisonState", "RWTexture2D", "RWTexture3D",
            "StructuredBuffer", "RWStructuredBuffer", "ByteAddressBuffer", "RWByteAddressBuffer",

            // ShaderLab property types
            "Color", "Vector", "Range", "2D", "3D", "Cube", "Float", "Int",

            // Common Unity macros/types
            "UnityObjectToClipPos", "UnityObjectToWorldNormal", "appdata", "v2f", "fragOutput"
        };

        static readonly Regex TokenRegex = new Regex(
            @"(?<comment>//[^\n]*|/\*[\s\S]*?\*/)" +
            @"|(?<string>""(?:\\.|[^""\\\n])*"")" +
            @"|(?<preproc>^\s*#\w+[^\n]*)" +
            @"|(?<number>\b\d+(?:\.\d+)?[fFhHd]?\b)" +
            @"|(?<semantic>:\s*[A-Z]\w*\b)" +
            @"|(?<word>\b[A-Za-z_]\w*\b)",
            RegexOptions.Compiled | RegexOptions.Multiline);

        public static string Highlight(string code)
        {
            if (string.IsNullOrEmpty(code)) return code;

            var sb = new StringBuilder(code.Length + 256);
            int pos = 0;

            foreach (Match m in TokenRegex.Matches(code))
            {
                if (m.Index > pos)
                    SyntaxHighlightUtil.AppendEscaped(sb, code, pos, m.Index - pos);

                var color = DetermineColor(m, code);
                SyntaxHighlightUtil.AppendToken(sb, m.Value, color);
                pos = m.Index + m.Length;
            }

            if (pos < code.Length)
                SyntaxHighlightUtil.AppendEscaped(sb, code, pos, code.Length - pos);

            return sb.ToString();
        }

        static string DetermineColor(Match m, string fullCode)
        {
            if (m.Groups["comment"].Success) return SyntaxHighlightUtil.ColorComment;
            if (m.Groups["string"].Success) return SyntaxHighlightUtil.ColorString;
            if (m.Groups["preproc"].Success) return SyntaxHighlightUtil.ColorPreproc;
            if (m.Groups["number"].Success) return SyntaxHighlightUtil.ColorNumber;
            if (m.Groups["semantic"].Success) return SyntaxHighlightUtil.ColorAttribute;
            if (m.Groups["word"].Success)
            {
                var word = m.Value;
                if (Keywords.Contains(word)) return SyntaxHighlightUtil.ColorKeyword;
                if (Types.Contains(word)) return SyntaxHighlightUtil.ColorType;

                int endIdx = m.Index + m.Length;
                while (endIdx < fullCode.Length && (fullCode[endIdx] == ' ' || fullCode[endIdx] == '\t'))
                    endIdx++;
                if (endIdx < fullCode.Length && fullCode[endIdx] == '(')
                    return SyntaxHighlightUtil.ColorMethod;
            }
            return null;
        }

        public static bool IsShaderLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang)) return false;
            switch (lang.ToLowerInvariant())
            {
                case "shader":
                case "shaderlab":
                case "hlsl":
                case "cg":
                case "glsl":
                    return true;
                default:
                    return false;
            }
        }
    }
}
