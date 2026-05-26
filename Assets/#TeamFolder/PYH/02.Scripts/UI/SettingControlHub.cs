using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.Enum;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using _TeamFolder.PYH._02.Scripts.Util;
using UnityEngine;
using UnityEngine.Audio;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class SettingControlHub : MonoSingleton<SettingControlHub>
    {
        [SerializeField] private SettingUiControlHub settingUi;
        [SerializeField] private AudioMixer mixer;
        
        private float _masterDB;
        private float _bgmDB;
        private float _sfxDB;
        
        private bool _isOpenSound;
        private bool _isOpenDisplay;
        
        private new void Awake()
        {
            base.Awake();
            settingUi.OnSettingVolumeSaveEvent += SettingVolumeVolumeSave;
            AStarEventBus.Subscribe<SettingUiEvent>(InteractSetting);
            AStarEventBus.Subscribe<SettingPopupUiEvent>(OpenPopup);

            _masterDB = PlayerPrefs.GetFloat("MasterDB", _masterDB);
            _bgmDB = PlayerPrefs.GetFloat("BGMDB", _bgmDB);
            _sfxDB = PlayerPrefs.GetFloat("SFXDB", _sfxDB);
        }
        private void Start()
        {
            mixer.SetFloat("Master", PlayerPrefs.GetFloat("MasterDB", _masterDB));
            mixer.SetFloat("BGM", PlayerPrefs.GetFloat("BGMDB", _bgmDB));
            mixer.SetFloat("SFX", PlayerPrefs.GetFloat("SFXDB", _sfxDB));
            
            Debug.Log("MASTER" + PlayerPrefs.GetFloat("MasterDB", _masterDB));
            Debug.Log("BGM" + PlayerPrefs.GetFloat("BGMDB", _bgmDB));
            Debug.Log("SFX" + PlayerPrefs.GetFloat("SFXDB", _sfxDB));
        }

        private void OnDestroy()
        {
            settingUi.OnSettingVolumeSaveEvent -= SettingVolumeVolumeSave;
            AStarEventBus.Unsubscribe<SettingUiEvent>(InteractSetting);
            AStarEventBus.Unsubscribe<SettingPopupUiEvent>(OpenPopup);
        }

        private void OnApplicationQuit()
        {
            Debug.Log("QUIT MASTER" + PlayerPrefs.GetFloat("MasterDB", _masterDB));
            Debug.Log("QUIT BGM" + PlayerPrefs.GetFloat("BGMDB", _bgmDB));
            Debug.Log("QUIT SFX" + PlayerPrefs.GetFloat("SFXDB", _sfxDB));
        }
        
        public void InteractSetting(SettingUiEvent @event)
        {
            AStarEventBus.Publish(new UiInteractEvent(settingUi));
        }
        public void InteractSetting() // for ui button on click event
        {
            AStarEventBus.Publish(new UiInteractEvent(settingUi));
        }

        private void SettingVolumeVolumeSave(float masterVolume, float bgmVolume, float sfxVolume)
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.Save();
        }
        
        private void OpenPopup(SettingPopupUiEvent @event) => settingUi.OpenPopup(@event.PopupType);

        public void MasterLabel(float amount)
        {
            settingUi.SetSoundLabel(SoundLabelEnum.MASTER, amount);
        }
        public void BGMLabel(float amount)
        {
            settingUi.SetSoundLabel(SoundLabelEnum.BGM, amount);
        }
        public void SfxLabel(float amount)
        {
            settingUi.SetSoundLabel(SoundLabelEnum.SFX, amount);
        }

        public void SetMasterVolume(float amount)
        {
            float dB;

            if (amount <= 0.0001f)
            {
                dB = -80f;
            }
            else
            {
                dB = Mathf.Log10(amount) * 20f;
            }

            dB = Mathf.Clamp(dB, -80f, 0f);
            
            mixer.SetFloat("Master", dB);

            PlayerPrefs.SetFloat("MasterVolume", amount);
            PlayerPrefs.SetFloat("MasterDB", dB);
            PlayerPrefs.Save();
        }
        public void SetBGMVolume(float amount)
        {
            float dB;

            if (amount <= 0.0001f)
            {
                dB = -80f;
            }
            else
            {
                dB = Mathf.Log10(amount) * 20f;
            }
            
            dB = Mathf.Clamp(dB, -80f, 0f);
            
            mixer.SetFloat("BGM", dB);

            PlayerPrefs.SetFloat("BGMVolume", amount);
            PlayerPrefs.SetFloat("BGMDB", dB);
            PlayerPrefs.Save();
        }
        public void SetSfxVolume(float amount)
        {
            float dB;

            if (amount <= 0.0001f)
            {
                dB = -80f;
            }
            else
            {
                dB = Mathf.Log10(amount) * 20f;
            }

            dB = Mathf.Clamp(dB, -80f, 0f);
            
            mixer.SetFloat("SFX", dB);

            PlayerPrefs.SetFloat("SFXVolume", amount);
            PlayerPrefs.SetFloat("SFXDB", dB);
            PlayerPrefs.Save();
        }
    }
}
