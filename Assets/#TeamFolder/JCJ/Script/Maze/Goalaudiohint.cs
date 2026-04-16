using System.Collections;
using UnityEngine;
 
namespace _TeamFolder.JCJ.Script
{
    [RequireComponent(typeof(AudioSource))]
    public class GoalAudioHint : MonoBehaviour, IGoalAudioHint
    {
        private GoalAudioSettings _settings;
        private AudioSource _audioSource;
        private Coroutine _hintCoroutine;
        private float _currentInterval;
 
        public void Inject(GoalAudioSettings settings)
        {
            _settings = settings;
            ApplyAudioSettings();
        }
        public void StartHint()
        {
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
            _audioSource.Stop();
        }
 
        public void SetInterval(float interval)
        {
            _currentInterval = interval;
            if (_hintCoroutine != null) StartHint();
        }
 
        private void ApplyAudioSettings()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.clip = _settings.hintClip;
            _audioSource.spatialBlend = 1.0f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.minDistance = _settings.minDistance;
            _audioSource.maxDistance = _settings.maxDistance;
            _audioSource.playOnAwake = false;
            _currentInterval = _settings.interval;
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