using TMPro;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace _TeamFolder.JCJ.Script.Arena.Editor
{
    [InitializeOnLoad]
    public static class ArenaKoreanFontBootstrap
    {
        private const string AssetPath = "Assets/#TeamFolder/JCJ/Resources/Arena/ArenaKoreanFont.asset";

        static ArenaKoreanFontBootstrap()
        {
            EditorApplication.delayCall += EnsureFontAsset;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += EnsureFontAsset;
        }

        [MenuItem("JCJ/Create Arena Korean TMP Font")]
        public static void EnsureFontAsset()
        {
            EditorApplication.delayCall -= EnsureFontAsset;
            if (!AssetDatabase.IsValidFolder("Assets/#TeamFolder/JCJ/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/#TeamFolder/JCJ", "Resources");
            }

            if (!AssetDatabase.IsValidFolder("Assets/#TeamFolder/JCJ/Resources/Arena"))
            {
                AssetDatabase.CreateFolder("Assets/#TeamFolder/JCJ/Resources", "Arena");
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            if (existing != null)
            {
                return;
            }

            var font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 16);
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("맑은 고딕", 16);
            }

            if (font == null)
            {
                return;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(font);
            fontAsset.name = "ArenaKoreanFont";
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(fontAsset, AssetPath);
            AssetDatabase.SaveAssets();
        }
    }
}
