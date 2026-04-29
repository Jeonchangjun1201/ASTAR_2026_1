using UnityEditor;
using UnityEngine;

namespace _TeamFolder.JCJ.Script.Arena.Editor
{
    [InitializeOnLoad]
    public static class ArenaAnimationLibraryBootstrap
    {
        private const string AssetPath = "Assets/#TeamFolder/JCJ/Resources/Arena/ArenaAnimationLibrary.asset";
        private const string PushPath = "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Animations/Punching.fbx";
        private const string ThrowPath = "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Animations/Throw.fbx";
        private const string CarryIdlePath = "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Animations/Box Idle.fbx";
        private const string CarryMovePath = "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Animations/Box Walk Arc.fbx";

        static ArenaAnimationLibraryBootstrap()
        {
            EditorApplication.delayCall += EnsureAsset;
        }

        [MenuItem("JCJ/Create Arena Animation Library")]
        public static void EnsureAsset()
        {
            EditorApplication.delayCall -= EnsureAsset;
            if (!AssetDatabase.IsValidFolder("Assets/#TeamFolder/JCJ/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/#TeamFolder/JCJ", "Resources");
            }

            if (!AssetDatabase.IsValidFolder("Assets/#TeamFolder/JCJ/Resources/Arena"))
            {
                AssetDatabase.CreateFolder("Assets/#TeamFolder/JCJ/Resources", "Arena");
            }

            var asset = AssetDatabase.LoadAssetAtPath<ArenaAnimationLibrary>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ArenaAnimationLibrary>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }

            var so = new SerializedObject(asset);
            SetClip(so, "_pushClip", PushPath, "Push");
            SetClip(so, "_throwClip", ThrowPath, "Throw");
            SetClip(so, "_carryIdleClip", CarryIdlePath, "CarryIdle");
            SetClip(so, "_carryMoveClip", CarryMovePath, "CarryMove");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static void SetClip(SerializedObject serializedObject, string propertyName, string assetPath, string clipName)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            AnimationClip clip = null;
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip animationClip && animationClip.name == clipName)
                {
                    clip = animationClip;
                    break;
                }
            }

            if (clip != null)
            {
                property.objectReferenceValue = clip;
            }
        }
    }
}
