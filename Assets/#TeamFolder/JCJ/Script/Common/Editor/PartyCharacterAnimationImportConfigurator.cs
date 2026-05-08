using UnityEditor;
using UnityEngine;

namespace _TeamFolder.JCJ.Script.Editor
{
    [InitializeOnLoad]
    public static class PartyCharacterAnimationImportConfigurator
    {
        private const string SessionKey = "JCJ.PartyCharacterAnimationImportConfigurator.Ran";

        private static readonly AnimationImportConfig[] Configs =
        {
            new(
                "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Animations/Throw.fbx",
                "Throw",
                false),
            new(
                "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Animations/Box Idle.fbx",
                "CarryIdle",
                true),
            new(
                "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Animations/Box Walk Arc.fbx",
                "CarryMove",
                true),
        };

        static PartyCharacterAnimationImportConfigurator()
        {
            EditorApplication.delayCall += RunOnce;
        }

        [MenuItem("JCJ/Configure Party Character Animation Imports")]
        public static void ConfigureFromMenu()
        {
            ConfigureAll(true);
        }

        private static void RunOnce()
        {
            EditorApplication.delayCall -= RunOnce;
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            ConfigureAll(false);
        }

        private static void ConfigureAll(bool force)
        {
            foreach (var config in Configs)
            {
                ConfigureAsset(config, force);
            }
        }

        private static void ConfigureAsset(AnimationImportConfig config, bool force)
        {
            var importer = AssetImporter.GetAtPath(config.AssetPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                changed = true;
            }

            if (importer.importCameras)
            {
                importer.importCameras = false;
                changed = true;
            }

            if (importer.importLights)
            {
                importer.importLights = false;
                changed = true;
            }

            if (!TryResolveSourceClip(importer, out var configuredClip))
            {
                if (changed)
                {
                    importer.SaveAndReimport();
                }
                return;
            }
            configuredClip.name = config.ClipName;
            configuredClip.loop = config.Loop;
            configuredClip.loopTime = config.Loop;
            configuredClip.loopPose = config.Loop;
            configuredClip.keepOriginalOrientation = true;
            configuredClip.keepOriginalPositionXZ = true;
            configuredClip.keepOriginalPositionY = true;
            configuredClip.lockRootRotation = true;
            configuredClip.lockRootPositionXZ = true;
            configuredClip.lockRootHeightY = true;
            configuredClip.wrapMode = config.Loop ? WrapMode.Loop : WrapMode.Once;

            if (force || !HasMatchingClip(importer, configuredClip))
            {
                importer.clipAnimations = new[] { configuredClip };
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static bool TryResolveSourceClip(ModelImporter importer, out ModelImporterClipAnimation clip)
        {
            if (importer.clipAnimations != null && importer.clipAnimations.Length > 0)
            {
                clip = importer.clipAnimations[0];
                return true;
            }

            if (importer.defaultClipAnimations != null && importer.defaultClipAnimations.Length > 0)
            {
                clip = importer.defaultClipAnimations[0];
                return true;
            }

            clip = default;
            return false;
        }

        private static bool HasMatchingClip(ModelImporter importer, ModelImporterClipAnimation configuredClip)
        {
            if (importer.clipAnimations == null || importer.clipAnimations.Length == 0)
            {
                return false;
            }

            var existing = importer.clipAnimations[0];
            return existing.name == configuredClip.name
                && existing.loop == configuredClip.loop
                && existing.loopTime == configuredClip.loopTime
                && existing.loopPose == configuredClip.loopPose
                && existing.keepOriginalOrientation == configuredClip.keepOriginalOrientation
                && existing.keepOriginalPositionXZ == configuredClip.keepOriginalPositionXZ
                && existing.keepOriginalPositionY == configuredClip.keepOriginalPositionY
                && existing.lockRootRotation == configuredClip.lockRootRotation
                && existing.lockRootPositionXZ == configuredClip.lockRootPositionXZ
                && existing.lockRootHeightY == configuredClip.lockRootHeightY
                && existing.wrapMode == configuredClip.wrapMode;
        }

        private readonly struct AnimationImportConfig
        {
            public AnimationImportConfig(string assetPath, string clipName, bool loop)
            {
                AssetPath = assetPath;
                ClipName = clipName;
                Loop = loop;
            }

            public string AssetPath { get; }
            public string ClipName { get; }
            public bool Loop { get; }
        }
    }
}
