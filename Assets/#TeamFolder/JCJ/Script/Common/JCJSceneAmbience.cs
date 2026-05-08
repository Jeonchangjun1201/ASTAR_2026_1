using UnityEngine;
using UnityEngine.Rendering;

// 씬 공통 배경 연출과 분위기 설정을 담당하는 컴포넌트.

namespace _TeamFolder.JCJ.Presentation
{
    /// <summary>
    /// 빈 오브젝트에 붙여 미로/타일 씬 공통 분위기(앰비언트·안개·카메라 배경)만 살짝 맞춥니다.
    /// 씬에 하나 두고 프리셋만 고르면 됩니다.
    /// </summary>
    public sealed class JCJSceneAmbience : MonoBehaviour
    {
        public enum Preset
        {
            Custom,
            MazeCool,   // 차분한 쿨톤 + 얕은 안개
            TileVivid   // 타일판: 앰비언트 밝게, 안개 약하게
        }

        [SerializeField] private Preset preset = Preset.MazeCool;
        [SerializeField] private bool applyOnAwake = true;

        [Header("Ambient")]
        [SerializeField] private AmbientMode ambientMode = AmbientMode.Flat;
        [SerializeField] private Color ambientLight = new Color(0.32f, 0.36f, 0.44f);

        [Header("Fog (optional)")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private Color fogColor = new Color(0.12f, 0.14f, 0.2f);
        [SerializeField] private float fogDensity = 0.012f;

        [Header("Camera clear (Main Camera)")]
        [SerializeField] private bool tintCameraBackground = true;
        [SerializeField] private Color cameraBackground = new Color(0.06f, 0.07f, 0.1f);

        private void Awake()
        {
            if (!applyOnAwake)
                return;
            ApplyPresetIfNeeded();
            Apply();
        }

        private void ApplyPresetIfNeeded()
        {
            switch (preset)
            {
                case Preset.MazeCool:
                    ambientLight = new Color(0.30f, 0.34f, 0.42f);
                    fogColor = new Color(0.10f, 0.12f, 0.16f);
                    fogDensity = 0.014f;
                    cameraBackground = new Color(0.05f, 0.06f, 0.09f);
                    enableFog = true;
                    break;
                case Preset.TileVivid:
                    ambientLight = new Color(0.45f, 0.48f, 0.55f);
                    fogColor = new Color(0.08f, 0.09f, 0.14f);
                    fogDensity = 0.006f;
                    cameraBackground = new Color(0.04f, 0.05f, 0.08f);
                    enableFog = true;
                    break;
            }
        }

        /// <summary>런타임에서 프리셋만 바꾼 뒤 다시 적용할 때 사용.</summary>
        public void Apply()
        {
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambientLight;

            RenderSettings.fog = enableFog;
            if (enableFog)
            {
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = fogDensity;
            }

            if (tintCameraBackground && Camera.main != null)
                Camera.main.backgroundColor = cameraBackground;
        }
    }
}
