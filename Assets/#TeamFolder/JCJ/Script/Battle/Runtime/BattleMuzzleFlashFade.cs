using UnityEngine;

// 총구 화염 이펙트를 짧게 보여주고 사라지게 하는 처리.

namespace _TeamFolder.JCJ.Battle
{
    public class BattleMuzzleFlashFade : MonoBehaviour
    {
        private Light _light;
        private float _initialIntensity;
        private float _elapsed;
        private const float Duration = 0.12f;

        private void Awake()
        {
            _light = GetComponent<Light>();
            if (_light != null) _initialIntensity = _light.intensity;
        }

        private void Update()
        {
            if (_light == null) return;
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Duration);
            _light.intensity = Mathf.Lerp(_initialIntensity, 0f, t * t);
            if (t >= 1f) _light.enabled = false;
        }
    }
}
