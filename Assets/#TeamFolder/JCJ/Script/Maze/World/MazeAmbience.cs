using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//  미로 공간의 배경 연출과 환경 효과를 담당하는 컴포넌트.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 런타임에서 URP 글로벌 볼륨, 포스트 프로세싱, 조명, 안개를 구성해 미로 씬 분위기를 만든다.
    ///
    /// 미리 만든 VolumeProfile 에셋 없이 메모리에서 전부 생성하므로 스크립트만으로 같은 룩을 재현할 수 있다.
    /// </summary>
    public class MazeAmbience : MonoBehaviour
    {
        [Header("Post-processing")]
        [Tooltip("아래 값으로 글로벌 URP Volume과 프로필을 자동 생성.")]
        [SerializeField] private bool _buildVolume = true;
        [SerializeField] [Range(0f, 3f)]   private float _bloomIntensity   = 0.55f;
        [SerializeField] [Range(0f, 4f)]   private float _bloomThreshold   = 1.10f;
        [SerializeField] [Range(0f, 1f)]   private float _bloomScatter     = 0.72f;
        [SerializeField] [Range(0f, 1f)]   private float _vignetteIntensity  = 0.36f;
        [SerializeField] [Range(0.01f, 1f)] private float _vignetteSmoothness = 0.45f;
        [Tooltip("음수면 채도를 낮춰 단색에 가까워짐.")]
        [SerializeField] [Range(-100f, 100f)] private float _saturation   = -28f;
        [SerializeField] [Range(-100f, 100f)] private float _contrast     =  14f;
        [SerializeField] [Range(-2f,   2f)]   private float _postExposure =   0.10f;
        [SerializeField] [Range(0f, 1f)] private float _chromaticAberration = 0.10f;
        [SerializeField] [Range(0f, 1f)] private float _filmGrain           = 0.22f;

        [Header("Scene Lighting")]
        [SerializeField] private bool  _configureLighting = true;
        [SerializeField] private Color _sunColor     = new(0.98f, 0.96f, 0.90f);
        [SerializeField] private float _sunIntensity = 1.05f;
        [SerializeField] private Vector3 _sunEuler   = new(52f, -32f, 0f);
        [SerializeField] private Color _skyTint      = new(0.09f, 0.10f, 0.14f);
        [SerializeField] private Color _equatorTint  = new(0.06f, 0.07f, 0.09f);
        [SerializeField] private Color _groundTint   = new(0.03f, 0.03f, 0.04f);

        [Header("Fog")]
        [SerializeField] private bool  _enableFog  = true;
        [SerializeField] private Color _fogColor   = new(0.07f, 0.08f, 0.10f);
        [SerializeField] [Range(0f, 0.1f)] private float _fogDensity = 0.013f;

        [Header("Hero Rim Light (follows local player)")]
        [SerializeField] private bool  _addHeroLight   = true;
        [SerializeField] private Color _heroColor      = new(0.82f, 0.88f, 1.00f);
        [SerializeField] private float _heroIntensity  = 2.2f;
        [SerializeField] private float _heroRange      = 18f;
        [SerializeField] private float _heroHeight     = 5.0f;

        private Volume _volume;
        private VolumeProfile _profile;
        private Light _sun;
        private Light _heroLight;
        private Transform _heroTarget;

        // ───────── Public API ─────────
        public void AttachHeroLight(Transform target)
        {
            _heroTarget = target;
            if (_addHeroLight) EnsureHeroLight();
        }

        // ───────── Lifecycle ─────────
        private void Awake()
        {
            if (_configureLighting) ConfigureLighting();
            EnablePostProcessingOnMainCamera();
            if (_buildVolume)       BuildVolume();
        }

        private void LateUpdate()
        {
            if (_heroLight == null || _heroTarget == null) return;
            var p = _heroTarget.position;
            _heroLight.transform.position = new Vector3(p.x, p.y + _heroHeight, p.z);
        }

        private void OnDestroy()
        {
            if (_profile != null) Destroy(_profile);
        }

        // ───────── Lighting ─────────
        private void ConfigureLighting()
        {
            EnsureDirectionalLight();

            RenderSettings.ambientMode         = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = _skyTint;
            RenderSettings.ambientEquatorColor = _equatorTint;
            RenderSettings.ambientGroundColor  = _groundTint;

            if (_enableFog)
            {
                RenderSettings.fog        = true;
                RenderSettings.fogMode    = FogMode.ExponentialSquared;
                RenderSettings.fogColor   = _fogColor;
                RenderSettings.fogDensity = _fogDensity;
            }
        }

        private void EnsureDirectionalLight()
        {
            _sun = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) { _sun = l; break; }
            }
            if (_sun == null)
            {
                var go = new GameObject("Sun (auto)");
                go.transform.SetParent(transform, false);
                _sun = go.AddComponent<Light>();
                _sun.type = LightType.Directional;
            }

            _sun.color          = _sunColor;
            _sun.intensity      = _sunIntensity;
            _sun.transform.eulerAngles = _sunEuler;
            _sun.shadows        = LightShadows.Soft;
            _sun.shadowStrength = 0.75f;
            _sun.shadowBias     = 0.03f;
            _sun.shadowNormalBias = 0.4f;
        }

        private void EnsureHeroLight()
        {
            if (_heroLight != null) return;
            var go = new GameObject("HeroRimLight");
            go.transform.SetParent(transform, false);
            _heroLight = go.AddComponent<Light>();
            _heroLight.type      = LightType.Point;
            _heroLight.color     = _heroColor;
            _heroLight.intensity = _heroIntensity;
            _heroLight.range     = _heroRange;
            _heroLight.shadows   = LightShadows.None;
            _heroLight.renderMode = LightRenderMode.Auto;
        }

        // ───────── Post-processing ─────────
        private void EnablePostProcessingOnMainCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing         = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality  = AntialiasingQuality.High;
        }

        private void BuildVolume()
        {
            if (_volume != null) return;

            var go = new GameObject("MazeAmbienceVolume");
            go.transform.SetParent(transform, false);
            _volume          = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10f;
            _volume.weight   = 1f;

            _profile      = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "MazeAmbienceProfile";
            _volume.sharedProfile = _profile;

            AddBloom();
            AddVignette();
            AddColorAdjustments();
            AddTonemapping();
            AddChromaticAberration();
            AddFilmGrain();
        }

        private void AddBloom()
        {
            var b = _profile.Add<Bloom>(true);
            b.intensity.Override(_bloomIntensity);
            b.threshold.Override(_bloomThreshold);
            b.scatter.Override(_bloomScatter);
            b.tint.Override(new Color(0.94f, 0.96f, 1.00f));
            b.highQualityFiltering.Override(true);
        }

        private void AddVignette()
        {
            var v = _profile.Add<Vignette>(true);
            v.intensity.Override(_vignetteIntensity);
            v.smoothness.Override(_vignetteSmoothness);
            v.color.Override(new Color(0f, 0f, 0f, 1f));
            v.rounded.Override(false);
        }

        private void AddColorAdjustments()
        {
            var c = _profile.Add<ColorAdjustments>(true);
            c.postExposure.Override(_postExposure);
            c.contrast.Override(_contrast);
            c.saturation.Override(_saturation);
            c.colorFilter.Override(new Color(0.97f, 0.98f, 1.00f));
        }

        private void AddTonemapping()
        {
            var t = _profile.Add<Tonemapping>(true);
            t.mode.Override(TonemappingMode.Neutral);
        }

        private void AddChromaticAberration()
        {
            var ca = _profile.Add<ChromaticAberration>(true);
            ca.intensity.Override(_chromaticAberration);
        }

        private void AddFilmGrain()
        {
            var fg = _profile.Add<FilmGrain>(true);
            fg.type.Override(FilmGrainLookup.Thin1);
            fg.intensity.Override(_filmGrain);
            fg.response.Override(0.8f);
        }
    }
}
