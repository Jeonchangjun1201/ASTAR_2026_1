using System.Collections.Generic;
using System;
using UnityEngine;
using PYH.Util;


namespace JHJ.Scripts.SoundManager
{
    public class SoundManager : MonoSingleton<SoundManager>
    {
        private Dictionary<Sound, AudioClip> SoundClipDictionary = new();

        private AudioSource _audioSource;
        private AudioSource _vfxSource;
        private AudioSource _vfxLoopSource;
        private AudioClip _audioClip;


        private float _volume = 0.5f;
        protected override void Awake()
        {
            base.Awake();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
            EnsureVfxSource();
            EnsureVfxLoopSource();
        }

        private void EnsureVfxSource()
        {
            if (_vfxSource != null) return;
            _vfxSource = gameObject.AddComponent<AudioSource>();
            _vfxSource.playOnAwake = false;
            _vfxSource.spatialBlend = 0f;
        }

        private void EnsureVfxLoopSource()
        {
            if (_vfxLoopSource != null) return;
            _vfxLoopSource = gameObject.AddComponent<AudioSource>();
            _vfxLoopSource.playOnAwake = false;
            _vfxLoopSource.loop = true;
            _vfxLoopSource.spatialBlend = 0f;
        }
        private void Start()
        {
            foreach (Sound s in Enum.GetValues(typeof(Sound)))//Enum에게서 (GetValues)값을 가져오겠다. 어떤 타입의?(typeof)Sound enum의 값을.
            {
                SoundClipDictionary[s] = Resources.Load<AudioClip>(s.ToString());
            }
        }

        public void PlaySound(Sound sound)//오디오 소스 재생
        {
            _audioSource.PlayOneShot(SoundClipDictionary[sound], _volume);//한번 재생하되, 재생되고 있던 클립을 멈추지 않음
            //_audioSource.Play();//한번 재생을 시작하며, 재생되고 있던 클립을 멈춤
        }

        public void IncreaseVolume()
        {
            _volume += 0.1f;
            _volume = Mathf.Clamp01(_volume);
        }

        public void DecreaseVolume()
        {
            _volume -= 0.1f;
            _volume = Mathf.Clamp01(_volume);
        }

        public float GetVolume()
        {
            return _volume;
        }

        /// <summary>레거시 PlaySound·JCJ 공통 출력 게인(보통 MASTER).</summary>
        public void SetOutputVolume(float normalized)
        {
            _volume = Mathf.Clamp01(normalized);
        }

        /// <summary>
        /// 2D 효과음 1회 재생. effectiveVolume은 호출 측에서 MASTER×VFX×씬 트림 등을
        /// 이미 곱한 최종 배율(0~1)로 넘긴다.
        /// </summary>
        public void PlayVfxClip(AudioClip clip, float effectiveVolume, float pitch = 1f)
        {
            if (clip == null) return;
            EnsureVfxSource();
            _vfxSource.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            _vfxSource.PlayOneShot(clip, Mathf.Clamp01(effectiveVolume));
        }

        /// <summary>2D 루프 VFX(발소리 등). 이미 재생 중이면 볼륨·피치만 갱신.</summary>
        public void PlayVfxLoop(AudioClip clip, float effectiveVolume, float pitch = 1f)
        {
            if (clip == null) return;
            EnsureVfxLoopSource();
            _vfxLoopSource.clip = clip;
            _vfxLoopSource.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            _vfxLoopSource.volume = Mathf.Clamp01(effectiveVolume);
            if (!_vfxLoopSource.isPlaying)
                _vfxLoopSource.Play();
        }

        public void StopVfxLoop()
        {
            if (_vfxLoopSource == null) return;
            _vfxLoopSource.Stop();
        }

        public bool IsVfxLoopPlaying => _vfxLoopSource != null && _vfxLoopSource.isPlaying;
    }

    public enum Sound
    {

    }
}


