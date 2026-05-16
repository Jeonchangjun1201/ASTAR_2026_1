using System.Linq;
using UnityEditor;
using UnityEngine;

namespace _TeamFolder.JCJ.Script.Editor
{
    internal static class CharacterModelImportPaths
    {
        public const string PartyCharacter =
            "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Models/party_character.fbx";
        public const string BattleCharacter =
            "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Models/battle_charactor.fbx";
    }

    [InitializeOnLoad]
    public static class PartyCharacterAnimationImportConfigurator
    {
        private const string InitializedSessionKey = "JCJ.PartyCharacterAnimationImportConfigurator.Initialized";

        private static readonly AnimationImportConfig[] PartyAnimationConfigs =
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
            new(
                "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Animations/Punching.fbx",
                "Push",
                false),
        };

        private static readonly AnimationImportConfig[] BattleAnimationConfigs =
        {
            new(
                "Assets/#TeamFolder/JCJ/Shooter Pack/rifle aiming idle.fbx",
                "RifleAimingIdle",
                true),
            new(
                "Assets/#TeamFolder/JCJ/Shooter Pack/walking.fbx",
                "Walking",
                true),
            new(
                "Assets/#TeamFolder/JCJ/Shooter Pack/rifle run.fbx",
                "RifleRun",
                true),
            new(
                "Assets/#TeamFolder/JCJ/Shooter Pack/stop walking.fbx",
                "StopWalking",
                false),
        };

        static PartyCharacterAnimationImportConfigurator()
        {
            EditorApplication.delayCall += RunOnce;
        }

        [MenuItem("JCJ/Configure Party Character Animation Imports")]
        public static void ConfigurePartyFromMenu()
        {
            ApplyAnimationConfigs(PartyAnimationConfigs, true);
        }

        [MenuItem("JCJ/Configure Battle TPS Animation Imports")]
        public static void ConfigureBattleFromMenu()
        {
            ApplyAnimationConfigs(BattleAnimationConfigs, true);
        }

        private static void RunOnce()
        {
            EditorApplication.delayCall -= RunOnce;
            if (SessionState.GetBool(InitializedSessionKey, false))
                return;

            SessionState.SetBool(InitializedSessionKey, true);
            ApplyAnimationConfigs(PartyAnimationConfigs, false);
            ApplyAnimationConfigs(BattleAnimationConfigs, false);
        }

        private static void ApplyAnimationConfigs(AnimationImportConfig[] configs, bool force)
        {
            if (!EnsureCharacterModelRigs())
                return;

            var avatar = LoadHumanoidAvatarFromModel(CharacterModelImportPaths.BattleCharacter);
            if (avatar == null)
                return;

            foreach (var config in configs)
                ConfigureAnimationAsset(config, avatar, force);
        }

        private static bool EnsureCharacterModelRigs()
        {
            return EnsureModelRig(
                       CharacterModelImportPaths.BattleCharacter,
                       ModelImporterAnimationType.Human,
                       ModelImporterAvatarSetup.CreateFromThisModel)
                   && EnsureModelRig(
                       CharacterModelImportPaths.PartyCharacter,
                       ModelImporterAnimationType.Generic,
                       ModelImporterAvatarSetup.CreateFromThisModel);
        }

        private static bool EnsureModelRig(
            string modelPath,
            ModelImporterAnimationType animationType,
            ModelImporterAvatarSetup avatarSetup)
        {
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
                return false;

            var changed = false;
            if (importer.animationType != animationType)
            {
                importer.animationType = animationType;
                changed = true;
            }

            if (importer.avatarSetup != avatarSetup)
            {
                importer.avatarSetup = avatarSetup;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();

            return true;
        }

        private static Avatar LoadHumanoidAvatarFromModel(string modelPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>()
                .FirstOrDefault(a => a != null && a.isValid && a.isHuman);
        }

        private static void ConfigureAnimationAsset(AnimationImportConfig config, Avatar sourceAvatar, bool force)
        {
            var importer = AssetImporter.GetAtPath(config.AssetPath) as ModelImporter;
            if (importer == null)
                return;

            var changed = false;

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                changed = true;
            }

            if (importer.sourceAvatar != sourceAvatar)
            {
                importer.sourceAvatar = sourceAvatar;
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
                    importer.SaveAndReimport();

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
                importer.SaveAndReimport();
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
                return false;

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

    internal sealed class CharacterModelRigImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null)
                return;

            if (assetPath == CharacterModelImportPaths.BattleCharacter)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                return;
            }

            if (assetPath == CharacterModelImportPaths.PartyCharacter)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
        }
    }
}
