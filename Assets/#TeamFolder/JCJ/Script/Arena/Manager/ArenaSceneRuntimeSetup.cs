using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Script.Arena
{
    public sealed class ArenaSceneRuntimeSetup : MonoBehaviour
    {
        [SerializeField] private float _arenaRadius = 18f;
        [SerializeField] private float _arenaFloorDepth = 14f;
        [SerializeField] private float _spawnRadius = 9f;
        [SerializeField] private Color _ambientColor = new Color(0.22f, 0.25f, 0.32f, 1f);
        [SerializeField] private Color _fogColor = new Color(0.05f, 0.06f, 0.09f, 1f);
        [SerializeField] private float _fogDensity = 0.0145f;
        [SerializeField] private Color _cameraBackground = new Color(0.03f, 0.04f, 0.06f, 1f);

        private bool _applied;

        private readonly ArenaFloorSpawnSpec[] _floorSpawnSpecs =
        {
            new ArenaFloorSpawnSpec("CarryItem_Light_01", new Vector3(-8.2f, 1.04f, -5.6f), new Vector3(0.60f, 0.34f, 0.72f)),
            new ArenaFloorSpawnSpec("CarryItem_Light_02", new Vector3(-5.6f, 1.00f, -9.4f), new Vector3(0.42f, 0.34f, 0.50f)),
            new ArenaFloorSpawnSpec("CarryItem_Light_03", new Vector3(0.4f, 1.08f, -8.6f), new Vector3(0.74f, 0.34f, 0.88f)),
            new ArenaFloorSpawnSpec("CarryItem_Light_04", new Vector3(7.1f, 1.00f, -5.8f), new Vector3(0.42f, 0.34f, 0.50f)),
            new ArenaFloorSpawnSpec("CarryItem_Light_05", new Vector3(8.0f, 1.04f, 4.8f), new Vector3(0.56f, 0.34f, 0.67f)),
            new ArenaFloorSpawnSpec("CarryItem_Light_06", new Vector3(3.8f, 1.08f, 8.9f), new Vector3(0.78f, 0.34f, 0.94f)),
            new ArenaFloorSpawnSpec("CarryItem_Light_07", new Vector3(-4.4f, 1.01f, 8.6f), new Vector3(0.46f, 0.34f, 0.55f)),
            new ArenaFloorSpawnSpec("CarryItem_Light_08", new Vector3(-8.7f, 1.00f, 4.1f), new Vector3(0.38f, 0.34f, 0.46f)),
            new ArenaFloorSpawnSpec("CarryItem_Heavy_01", new Vector3(-2.8f, 1.11f, -1.2f), new Vector3(0.96f, 0.34f, 1.15f)),
            new ArenaFloorSpawnSpec("CarryItem_Heavy_02", new Vector3(3.1f, 1.11f, 1.4f), new Vector3(0.96f, 0.34f, 1.15f)),
            new ArenaFloorSpawnSpec("CarryItem_Heavy_03", new Vector3(0.5f, 1.06f, 6.0f), new Vector3(0.58f, 0.34f, 0.70f)),
            new ArenaFloorSpawnSpec("CarryItem_Heavy_04", new Vector3(-0.8f, 1.09f, -6.1f), new Vector3(0.84f, 0.34f, 1.00f))
        };

        public void ApplyRuntimeSetup()
        {
            if (_applied)
            {
                return;
            }

            _applied = true;
            EnsureMazeCameraService();
            ApplyArenaDimensions();
            EnsureArenaColliders();
            EnsureSpawnMarkers();
            LayoutFloorSpawns();
            EnsurePostProcessing();
            ApplyEnvironment();
        }

        private void EnsureMazeCameraService()
        {
            Camera targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindFirstObjectByType<Camera>();
            }

            if (targetCamera == null)
            {
                return;
            }

            var arenaRig = targetCamera.GetComponent<ArenaCameraRig>();
            if (arenaRig != null)
            {
                Destroy(arenaRig);
            }

            if (targetCamera.GetComponent<PlayerFollowCameraService>() == null)
            {
                targetCamera.gameObject.AddComponent<PlayerFollowCameraService>();
            }
        }

        private void ApplyArenaDimensions()
        {
            ApplyDiskScale("ArenaDisk", _arenaRadius, 0f, 0.9f);
            ApplyDiskScale("ArenaLowerPlane", _arenaRadius + _arenaFloorDepth, -9f, 1f);
        }

        private void ApplyDiskScale(string objectName, float radius, float y, float height)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null)
            {
                return;
            }

            target.transform.position = new Vector3(0f, y, 0f);
            target.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        }

        private void EnsureArenaColliders()
        {
            ConvertPrimitiveCollider("ArenaDisk");
            ConvertPrimitiveCollider("ArenaLowerPlane");
        }

        private void ConvertPrimitiveCollider(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null)
            {
                return;
            }

            var capsule = target.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                Destroy(capsule);
            }

            var meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            var meshCollider = target.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = target.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
        }

        private void EnsureSpawnMarkers()
        {
            var rootObject = GameObject.Find("ArenaSpawnMarkers");
            if (rootObject == null)
            {
                rootObject = new GameObject("ArenaSpawnMarkers");
            }

            var root = rootObject.transform;
            root.transform.SetParent(null, false);
            CreateOrMoveMarker(root, "SpawnMarker_1", new Vector3(-_spawnRadius, 0.06f, -_spawnRadius));
            CreateOrMoveMarker(root, "SpawnMarker_2", new Vector3(_spawnRadius, 0.06f, -_spawnRadius));
            CreateOrMoveMarker(root, "SpawnMarker_3", new Vector3(-_spawnRadius, 0.06f, _spawnRadius));
            CreateOrMoveMarker(root, "SpawnMarker_4", new Vector3(_spawnRadius, 0.06f, _spawnRadius));
        }

        private void CreateOrMoveMarker(Transform parent, string name, Vector3 position)
        {
            Transform existing = parent.Find(name);
            GameObject marker = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(1.8f, 0.04f, 1.8f);

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = BuildMaterial(new Color(0.77f, 0.16f, 0.18f, 1f), 0.15f);
            }
        }

        private void LayoutFloorSpawns()
        {
            for (int i = 0; i < _floorSpawnSpecs.Length; i++)
            {
                var floorSpawn = GameObject.Find(_floorSpawnSpecs[i].Name);
                if (floorSpawn == null)
                {
                    continue;
                }

                floorSpawn.transform.position = _floorSpawnSpecs[i].Position;
                floorSpawn.transform.localScale = _floorSpawnSpecs[i].Scale;
            }
        }

        private void EnsurePostProcessing()
        {
            var volumeObject = GameObject.Find("ArenaGlobalVolume");
            if (volumeObject == null)
            {
                volumeObject = new GameObject("ArenaGlobalVolume");
            }

            var volume = volumeObject.GetComponent<Volume>();
            if (volume == null)
            {
                volume = volumeObject.AddComponent<Volume>();
            }

            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;
            if (volume.sharedProfile == null)
            {
                volume.sharedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            }

            ConfigureBloom(volume.sharedProfile);
            ConfigureVignette(volume.sharedProfile);
            ConfigureColorAdjustments(volume.sharedProfile);
        }

        private void ConfigureBloom(VolumeProfile profile)
        {
            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }

            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.88f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.48f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.72f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(0.62f, 0.78f, 1f, 1f);
            bloom.clamp.overrideState = true;
            bloom.clamp.value = 65472f;
        }

        private void ConfigureVignette(VolumeProfile profile)
        {
            if (!profile.TryGet(out Vignette vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }

            vignette.active = true;
            vignette.color.overrideState = true;
            vignette.color.value = new Color(0.03f, 0.04f, 0.07f, 1f);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.28f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.82f;
            vignette.rounded.overrideState = true;
            vignette.rounded.value = true;
        }

        private void ConfigureColorAdjustments(VolumeProfile profile)
        {
            if (!profile.TryGet(out ColorAdjustments colorAdjustments))
            {
                colorAdjustments = profile.Add<ColorAdjustments>(true);
            }

            colorAdjustments.active = true;
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = -0.12f;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = 18f;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = -12f;
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.colorFilter.value = new Color(0.82f, 0.90f, 1f, 1f);
        }

        private void ApplyEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = _fogColor;
            RenderSettings.fogDensity = _fogDensity;

            Camera targetCamera = Camera.main;
            if (targetCamera != null)
            {
                targetCamera.backgroundColor = _cameraBackground;
            }

            var light = FindFirstObjectByType<Light>();
            if (light != null)
            {
                light.intensity = 1.28f;
                light.color = new Color(0.82f, 0.88f, 1f, 1f);
            }
        }

        private Material BuildMaterial(Color color, float emissionIntensity)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            material.color = color;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emissionIntensity);
            }

            return material;
        }

        public float GetArenaRadius()
        {
            return _arenaRadius;
        }

        private readonly struct ArenaFloorSpawnSpec
        {
            public readonly string Name;
            public readonly Vector3 Position;
            public readonly Vector3 Scale;

            public ArenaFloorSpawnSpec(string name, Vector3 position, Vector3 scale)
            {
                Name = name;
                Position = position;
                Scale = scale;
            }
        }
    }
}
