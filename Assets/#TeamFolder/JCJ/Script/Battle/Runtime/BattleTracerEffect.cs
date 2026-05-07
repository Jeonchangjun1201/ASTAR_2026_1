using UnityEngine;

namespace _TeamFolder.JCJ.Battle
{
    public class BattleTracerEffect : MonoBehaviour
        , IBattlePoolAware
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private LineRenderer _line;
        private Material _runtimeMaterial;
        private Vector3 _direction;
        private float _speed;
        private float _length;
        private float _headDistance;
        private float _maxDistance;
        private Vector3 _origin;
        private float _elapsed;
        private const float FadeOutTime = 0.06f;
        private float _fadeElapsed;
        private bool _fading;

        public void Initialize(Vector3 origin, Vector3 direction, float length, float speed, float width, Color color)
        {
            _origin = origin;
            _direction = direction.normalized;
            _speed = speed;
            _length = length;
            _maxDistance = speed * 0.25f;
            _headDistance = 0f;
            _elapsed = 0f;
            _fadeElapsed = 0f;
            _fading = false;

            EnsureLine();
            float visibleWidth = Mathf.Clamp(width * 0.75f, 0.008f, 0.02f);
            _line.startWidth = visibleWidth;
            _line.endWidth = visibleWidth * 0.72f;

            if (_runtimeMaterial == null) _runtimeMaterial = CreateRuntimeMaterial();
            Color neonColor = Color.Lerp(color, Color.white, 0.35f);
            neonColor.a = 1f;
            _runtimeMaterial.color = neonColor;
            if (_runtimeMaterial.HasProperty(BaseColorId)) _runtimeMaterial.SetColor(BaseColorId, neonColor);
            if (_runtimeMaterial.HasProperty(ColorId)) _runtimeMaterial.SetColor(ColorId, neonColor);
            _line.material = _runtimeMaterial;

            var colorGrad = new Gradient();
            colorGrad.SetKeys(
                new[] { new GradientColorKey(neonColor, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.22f, 0.65f), new GradientAlphaKey(0f, 1f) });
            _line.colorGradient = colorGrad;

            UpdateLinePositions();
        }

        private void Update()
        {
            if (_fading)
            {
                _fadeElapsed += Time.deltaTime;
                float fadeT = Mathf.Clamp01(_fadeElapsed / FadeOutTime);
                if (_line != null)
                {
                    Color startColor = _line.startColor;
                    startColor.a = Mathf.Lerp(1f, 0f, fadeT);
                    _line.startColor = startColor;
                    Color endColor = _line.endColor;
                    endColor.a = Mathf.Lerp(0.3f, 0f, fadeT);
                    _line.endColor = endColor;
                    _line.startWidth = Mathf.Lerp(_line.startWidth, 0f, fadeT);
                }
                if (fadeT >= 1f) BattlePoolManager.Release(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            _headDistance += _speed * Time.deltaTime;
            UpdateLinePositions();

            if (_headDistance >= _maxDistance)
            {
                _fading = true;
            }
        }

        private void UpdateLinePositions()
        {
            if (_line == null) return;
            Vector3 head = _origin + _direction * _headDistance;
            float tailDist = Mathf.Max(0f, _headDistance - _length);
            Vector3 tail = _origin + _direction * tailDist;
            _line.SetPosition(0, head);
            _line.SetPosition(1, tail);
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }

        public void OnSpawnedFromPool()
        {
            _elapsed = 0f;
            _fadeElapsed = 0f;
            _fading = false;
            if (_line != null) _line.enabled = true;
        }

        public void OnReturnedToPool()
        {
            _elapsed = 0f;
            _fadeElapsed = 0f;
            _fading = false;
            if (_line != null)
            {
                _line.enabled = false;
                _line.positionCount = 2;
            }
        }

        private void EnsureLine()
        {
            if (_line != null) return;

            _line = gameObject.GetComponent<LineRenderer>();
            if (_line == null) _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.alignment = LineAlignment.View;
            _line.textureMode = LineTextureMode.Stretch;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.positionCount = 2;
            _line.numCapVertices = 0;
            _line.numCornerVertices = 0;
        }

        private static Material CreateRuntimeMaterial()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("UI/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Hidden/InternalColored");

            var material = new Material(shader);
            material.renderQueue = 3100;
            return material;
        }
    }
}
