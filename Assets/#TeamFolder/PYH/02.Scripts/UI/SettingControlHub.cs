using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.Enum;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using _TeamFolder.PYH._02.Scripts.Util;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class SettingControlHub : MonoSingleton<SettingControlHub>
    {
        [SerializeField] private SettingUiControlHub settingUi;
        
        private bool _isOpenSound;
        private bool _isOpenDisplay;
        
        private new void Awake()
        {
            base.Awake();
            settingUi.OnSettingUiHide += SettingSave;
            AStarEventBus.Subscribe<SettingUiEvent>(InteractSetting);
            AStarEventBus.Subscribe<SettingPopupUiEvent>(OpenPopup);
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
            PlayerPrefs.SetFloat("MasterVolume", amount);
        }
        public void SetBGMVolume(float amount)
        {
            PlayerPrefs.SetFloat("BGMVolume", amount);
        }
        public void SetSfxVolume(float amount)
        {
            PlayerPrefs.SetFloat("SFXVolume", amount);
        }
    }
}
