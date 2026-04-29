using System.Collections;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 골 지점에서 3D 힌트 사운드를 일정 간격으로 반복 재생한다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GoalAudioHint : MonoBehaviour, IGoalAudioHint
    {
        private GoalAudioSettings _settings;
        private AudioSource _audioSource;
        private Coroutine _hintCoroutine;
        private float _currentInterval;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void Inject(GoalAudioSettings settings)
        {
            _settings = settings;
            ApplyAudioSettings();
        }

        public void StartHint()
        {
            if (_settings == null || _audioSource == null || _settings.hintClip == null) return;
            if (_hintCoroutine != null) StopCoroutine(_hintCoroutine);
            _hintCoroutine = StartCoroutine(HintLoop());
        }

        public void StopHint()
        {
            if (_hintCoroutine != null)
            {
                StopCoroutine(_hintCoroutine);
                _hintCoroutine = null;
            }
            if (_audioSource != null) _audioSource.Stop();
        }

        public void SetInterval(float interval)
        {
            _currentInterval = Mathf.Max(0.1f, interval);
            if (_hintCoroutine != null) StartHint();
        }

        private void ApplyAudioSettings()
        {
            // 골 위치를 방향성 힌트로 사용할 수 있도록 AudioSource를 3D 공간 사운드로 맞춘다.
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
            _audioSource.clip = _settings.hintClip;
            _audioSource.spatialBlend = 1.0f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.minDistance = _settings.minDistance;
            _audioSource.maxDistance = _settings.maxDistance;
            _audioSource.playOnAwake = false;
            _currentInterval = Mathf.Max(0.1f, _settings.interval);
        }

        private IEnumerator HintLoop()
        {
            while (true)
            {
                _audioSource.Play();
                yield return new WaitForSeconds(_currentInterval);
            }
        }
    }
}
