using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClaudeCode.Editor.UI
{
    /// <summary>
    /// Centralized access to Unity built-in editor icons.
    /// Uses dark-theme variants ("d_" prefix) since most users run dark theme.
    /// </summary>
    public static class EditorIcons
    {
        public static Texture2D Refresh   => Get("d_Refresh@2x");
        public static Texture2D Lock      => Get("d_LockIcon");
        public static Texture2D LockOn    => Get("d_LockIcon-On");
        public static Texture2D Account   => Get("d_Profiler.UIDetails");
        public static Texture2D Error     => Get("d_console.erroricon");
        public static Texture2D Warning   => Get("d_console.warnicon");
        public static Texture2D Info      => Get("d_console.infoicon");
        public static Texture2D Edit      => Get("d_editicon.sml");
        public static Texture2D Copy      => Get("d_Clipboard");
        public static Texture2D Settings  => Get("d_SettingsIcon");
        public static Texture2D Help      => Get("d_Help@2x");
        public static Texture2D Search    => Get("d_Search Icon");
        public static Texture2D Folder    => Get("d_Folder Icon");
        public static Texture2D Script    => Get("d_cs Script Icon");
        public static Texture2D Prefab    => Get("d_Prefab Icon");
        public static Texture2D Material  => Get("d_Material Icon");
        public static Texture2D Image     => Get("d_Texture2D Icon");
        public static Texture2D GameObject => Get("d_GameObject Icon");
        public static Texture2D Scene     => Get("d_SceneAsset Icon");
        public static Texture2D Close     => Get("d_winbtn_win_close");
        public static Texture2D Plus      => Get("d_Toolbar Plus");
        public static Texture2D Filter    => Get("d_FilterByLabel");
        public static Texture2D Play      => Get("d_PlayButton");

        static Texture2D Get(string name)
        {
            var content = EditorGUIUtility.IconContent(name);
            return content?.image as Texture2D;
        }

        /// <summary>
        /// Creates a small UI Toolkit Image element with the given Unity built-in icon.
        /// </summary>
        public static Image MakeIcon(Texture2D tex, int size = 14)
        {
            var img = new Image { image = tex };
            img.style.width = size;
            img.style.height = size;
            img.style.flexShrink = 0;
            return img;
        }
    }
}
