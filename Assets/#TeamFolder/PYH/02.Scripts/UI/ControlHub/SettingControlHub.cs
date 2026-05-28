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
        
        private const string MasterVolumeKey = "MasterVolume";
        private const string BGMVolumeKey = "BGMVolume";
        private const string SFXVolumeKey = "SFXVolume";

        private const string MasterMixerKey = "Master";
        private const string BGMMixerKey = "BGM";
        private const string SFXMixerKey = "SFX";
        
        private new void Awake()
        {
            base.Awake();
            settingUi.OnSettingVolumeSaveEvent += SettingVolumeVolumeSave;
            AStarEventBus.Subscribe<SettingUiEvent>(InteractSetting);
            AStarEventBus.Subscribe<SettingPopupUiEvent>(OpenPopup);
        }
        private void Start()
        {
            LoadVolume();
        }

        private void OnDestroy()
        {
            settingUi.OnSettingVolumeSaveEvent -= SettingVolumeVolumeSave;
            AStarEventBus.Unsubscribe<SettingUiEvent>(InteractSetting);
            AStarEventBus.Unsubscribe<SettingPopupUiEvent>(OpenPopup);
        }

        private void LoadVolume()
        {
            float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            float bgmVolume = PlayerPrefs.GetFloat(BGMVolumeKey, 1f);
            float sfxVolume = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);

            mixer.SetFloat(MasterMixerKey, ConvertVolumeToDB(masterVolume));
            mixer.SetFloat(BGMMixerKey, ConvertVolumeToDB(bgmVolume));
            mixer.SetFloat(SFXMixerKey, ConvertVolumeToDB(sfxVolume));
        }
        private float ConvertVolumeToDB(float amount)
        {
            if (amount <= 0.0001f)
                return -80f;

            return Mathf.Clamp(Mathf.Log10(amount) * 20f, -80f, 0f);
        }
        private void SaveVolume(string volumeKey, string mixerKey, float amount)
        {
            amount = Mathf.Clamp01(amount);

            PlayerPrefs.SetFloat(volumeKey, amount);
            mixer.SetFloat(mixerKey, ConvertVolumeToDB(amount));
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
            SaveVolume(MasterVolumeKey, MasterMixerKey, amount);
            PlayerPrefs.Save();
        }
        public void SetBGMVolume(float amount)
        {
            SaveVolume(BGMVolumeKey, BGMMixerKey, amount);
            PlayerPrefs.Save();
        }
        public void SetSfxVolume(float amount)
        {
            SaveVolume(SFXVolumeKey, SFXMixerKey, amount);
            PlayerPrefs.Save();
        }
    }
}
