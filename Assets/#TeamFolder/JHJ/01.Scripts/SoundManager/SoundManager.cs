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
        private AudioClip _audioClip;


        private float _volume = 0.5f;
        protected override void Awake()
        {
            base.Awake();
            _audioSource = GetComponent<AudioSource>();
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
    }

    public enum Sound
    {

    }
}


