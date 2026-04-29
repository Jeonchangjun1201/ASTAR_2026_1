using DG.Tweening;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 골 위치를 멀리서도 알아볼 수 있게 파티클 기둥과 깜빡이는 포인트 라이트를 만든다.
    /// </summary>
    public class GoalBeacon : MonoBehaviour
    {
        [Tooltip("기본 비콘 색. 차가운 단색 씬에서 눈에 띄도록 따뜻한 오프화이트 사용.")]
        [SerializeField] private Color _color = new(1f, 0.92f, 0.72f, 1f);
        [SerializeField] private float _columnHeight = 14f;

        private Light _light;
        private ParticleSystem _ps;
        private Tween _pulseTween;

        public void Build(Color color, float columnHeight = 14f)
        {
            _color = color;
            _columnHeight = columnHeight;

            BuildLight();
            BuildParticles();
        }

        private void BuildLight()
        {
            if (_light != null) return;
            var lightGo = new GameObject("BeaconLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0, 2f, 0);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = 14f;
            _light.intensity = 3f;
            _light.color = _color;
        }

        private void BuildParticles()
        {
            if (_ps != null) return;
            var psGo = new GameObject("BeaconParticles");
            psGo.transform.SetParent(transform, false);
            psGo.transform.localPosition = Vector3.zero;
            _ps = psGo.AddComponent<ParticleSystem>();

            var main = _ps.main;
            main.loop = true;
            main.startLifetime = 2.5f;
            main.startSpeed = 3f;
            main.startSize = 0.35f;
            main.startColor = _color;
            main.gravityModifier = -0.1f;
            main.maxParticles = 200;

            var emission = _ps.emission;
            emission.rateOverTime = 35f;

            var shape = _ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.4f;

            var colorOverLife = _ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(_color, 0f), new GradientColorKey(_color, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            var sizeOverLife = _ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            var curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var velocity = _ps.velocityOverLifetime;
            velocity.enabled = true;
            var zeroCurve = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.x = zeroCurve;
            velocity.z = zeroCurve;
            velocity.y = new ParticleSystem.MinMaxCurve(_columnHeight * 0.4f, _columnHeight * 0.6f);

            var renderer = _ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                       ?? Shader.Find("Particles/Standard Unlit"));
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", _color);
                mat.color = _color;
                renderer.material = mat;
            }
        }

        private void Start()
        {
            if (_light != null)
            {
                _pulseTween = DOTween.To(() => _light.intensity, v => _light.intensity = v, 4.5f, 1.1f)
                                     .SetLoops(-1, LoopType.Yoyo)
                                     .SetEase(Ease.InOutSine);
            }
        }

        private void OnDestroy()
        {
            _pulseTween?.Kill();
        }
    }
}
