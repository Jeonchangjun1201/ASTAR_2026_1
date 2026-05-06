using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _TeamFolder.JCJ.Script.Arena.Editor
{
    [InitializeOnLoad]
    public static class ArenaSceneBootstrap
    {
        private const string ScenePath = "Assets/#TeamFolder/JCJ/Scene/ArenaBattleScene.unity";
        private const string SessionKey = "JCJ.ArenaSceneBootstrap.Created";

        static ArenaSceneBootstrap()
        {
            EditorApplication.delayCall += RunOnce;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += RunOnce;
        }

        [MenuItem("JCJ/Create Arena Battle Scene")]
        public static void CreateOrUpdateScene()
        {
            BuildScene(forceRebuild: true);
        }

        private static void RunOnce()
        {
            EditorApplication.delayCall -= RunOnce;
            bool sceneExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            if (SessionState.GetBool(SessionKey, false) && sceneExists)
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            if (!sceneExists)
            {
                BuildScene(forceRebuild: true);
            }
        }

        private static void BuildScene(bool forceRebuild)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene.name = "ArenaBattleScene";

                CreateCamera();
                CreateLight();
                CreateArenaSystems();
                CreateArenaGeometry();
                CreateCarryItems();

                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 13f, -14f);
            cameraObject.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
        }

        private static void CreateArenaSystems()
        {
            var systemsObject = new GameObject("ArenaSystems");
            var manager = systemsObject.AddComponent<ArenaGameManager>();
            systemsObject.AddComponent<ArenaServerBridge>();
            systemsObject.AddComponent<ArenaSceneRuntimeSetup>();
            new GameObject("ArenaHud").AddComponent<ArenaPrepHud>();

            var managerSerialized = new SerializedObject(manager);
            managerSerialized.FindProperty("_autoStartOnPlay").boolValue = true;
            managerSerialized.FindProperty("_spawnCenter").vector3Value = new Vector3(0f, 1.2f, 0f);
            managerSerialized.FindProperty("_spawnRadius").floatValue = 9f;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateArenaGeometry()
        {
            var root = new GameObject("ArenaGeometry").transform;
            CreateDisk(root, "ArenaDisk", new Vector3(0f, 0f, 0f), 18f, 0.9f, new Color(0.06f, 0.06f, 0.08f));
            CreateDisk(root, "ArenaLowerPlane", new Vector3(0f, -9f, 0f), 32f, 1f, new Color(0.03f, 0.03f, 0.05f));
            CreateSpawnMarker(root, "SpawnMarker_1", new Vector3(-9f, 0.06f, -9f));
            CreateSpawnMarker(root, "SpawnMarker_2", new Vector3(9f, 0.06f, -9f));
            CreateSpawnMarker(root, "SpawnMarker_3", new Vector3(-9f, 0.06f, 9f));
            CreateSpawnMarker(root, "SpawnMarker_4", new Vector3(9f, 0.06f, 9f));
        }

        private static void CreateCarryItems()
        {
            var root = new GameObject("ArenaCarryItems").transform;
            var itemSpecs = new List<(string name, float x, float z, float scale, int strength, float throwPower, Color color)>
            {
                ("CarryItem_Light_01", -8.2f, -5.6f, 0.60f, 1, 10f, new Color(0.36f, 0.82f, 0.96f)),
                ("CarryItem_Light_02", -5.6f, -9.4f, 0.42f, 1, 10f, new Color(0.36f, 0.82f, 0.96f)),
                ("CarryItem_Light_03", 0.4f, -8.6f, 0.74f, 1, 10f, new Color(0.36f, 0.82f, 0.96f)),
                ("CarryItem_Light_04", 7.1f, -5.8f, 0.42f, 1, 10f, new Color(0.36f, 0.82f, 0.96f)),
                ("CarryItem_Light_05", 8.0f, 4.8f, 0.56f, 1, 10f, new Color(0.36f, 0.82f, 0.96f)),
                ("CarryItem_Light_06", 3.8f, 8.9f, 0.78f, 1, 10f, new Color(0.36f, 0.82f, 0.96f)),
                ("CarryItem_Light_07", -4.4f, 8.6f, 0.46f, 1, 10f, new Color(0.36f, 0.82f, 0.96f)),
                ("CarryItem_Light_08", -8.7f, 4.1f, 0.38f, 1, 10f, new Color(0.36f, 0.82f, 0.96f)),
                ("CarryItem_Heavy_01", -2.8f, -1.2f, 0.96f, 2, 14f, new Color(0.56f, 0.88f, 0.98f)),
                ("CarryItem_Heavy_02", 3.1f, 1.4f, 0.96f, 2, 14f, new Color(0.56f, 0.88f, 0.98f)),
                ("CarryItem_Heavy_03", 0.5f, 6.0f, 0.58f, 2, 14f, new Color(0.56f, 0.88f, 0.98f)),
                ("CarryItem_Heavy_04", -0.8f, -6.1f, 0.84f, 2, 14f, new Color(0.56f, 0.88f, 0.98f))
            };

            for (int i = 0; i < itemSpecs.Count; i++)
            {
                var spec = itemSpecs[i];
                CreateCarryItem(root, spec.name, new Vector3(spec.x, 1.00f + (spec.scale - 0.42f) * 0.20f, spec.z), new Vector3(spec.scale, 0.34f, spec.scale * 1.2f), spec.strength, spec.throwPower, spec.color);
            }
        }

        private static void CreateCarryItem(Transform parent, string name, Vector3 position, Vector3 scale, int requiredStrength, float throwPower, Color color)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.position = position;
            item.transform.localScale = scale;

            var renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = BuildMaterial(color);
            }

            var rigidbody = item.AddComponent<Rigidbody>();
            rigidbody.mass = Mathf.Max(1f, requiredStrength * 2f);
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var carryItem = item.AddComponent<ArenaCarryItem>();
            var serialized = new SerializedObject(carryItem);
            serialized.FindProperty("_itemId").stringValue = name;
            serialized.FindProperty("_requiredStrength").intValue = requiredStrength;
            serialized.FindProperty("_basePickupTime").floatValue = requiredStrength >= 2 ? 0.5f : 0.35f;
            serialized.FindProperty("_baseCarryMovePenaltyPercent").floatValue = requiredStrength >= 2 ? 0.18f : 0.10f;
            serialized.FindProperty("_baseThrowPower").floatValue = throwPower;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.SetParent(parent, false);
            platform.transform.position = position;
            platform.transform.localScale = scale;
            var renderer = platform.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = BuildMaterial(color);
            }
        }

        private static void CreateDisk(Transform parent, string name, Vector3 position, float radius, float height, Color color)
        {
            var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disk.name = name;
            disk.transform.SetParent(parent, false);
            disk.transform.position = position;
            disk.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            var renderer = disk.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = BuildMaterial(color);
            }

            var capsuleCollider = disk.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                Object.DestroyImmediate(capsuleCollider);
            }

            var meshFilter = disk.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var meshCollider = disk.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
            }
        }

        private static void CreateSpawnMarker(Transform parent, string name, Vector3 position)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(1.8f, 0.04f, 1.8f);
            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = BuildMaterial(new Color(0.72f, 0.18f, 0.20f));
            }

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Material BuildMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            material.color = color;
            return material;
        }
    }
}
