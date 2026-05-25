using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.Enum;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using _TeamFolder.PYH._02.Scripts.Util;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.Rendering.DebugUI;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class SettingControlHub : MonoSingleton<SettingControlHub>
    {
        [SerializeField] private SettingUiControlHub settingUi;
        [SerializeField] private AudioMixer mixer;

        private bool _isOpenSound;
        private bool _isOpenDisplay;
        
        private new void Awake()
        {
            base.Awake();
            settingUi.OnSettingUiHide += SettingSave;
            AStarEventBus.Subscribe<SettingUiEvent>(InteractSetting);
            AStarEventBus.Subscribe<SettingPopupUiEvent>(OpenPopup);

            mixer.SetFloat("Master", PlayerPrefs.GetFloat("MasterDB"));
            mixer.SetFloat("BGM", PlayerPrefs.GetFloat("BGMDB"));
            mixer.SetFloat("SFX", PlayerPrefs.GetFloat("SFXDB"));
        }
        private void OnDestroy()
        {
            settingUi.OnSettingUiHide -= SettingSave;
            AStarEventBus.Unsubscribe<SettingUiEvent>(InteractSetting);
            AStarEventBus.Unsubscribe<SettingPopupUiEvent>(OpenPopup);
        }
        
        public void InteractSetting(SettingUiEvent @event)
        {
            AStarEventBus.Publish(new UiInteractEvent(settingUi));
        }
        public void InteractSetting() // for ui button on click event
        {
            AStarEventBus.Publish(new UiInteractEvent(settingUi));
        }

        private void SettingSave(float masterVolume, float bgmVolume, float sfxVolume)
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
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
        }
    }
}
