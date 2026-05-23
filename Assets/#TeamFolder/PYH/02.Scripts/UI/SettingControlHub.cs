using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using _TeamFolder.PYH._02.Scripts.Util;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class SettingControlHub : MonoSingleton<SettingControlHub>
    {
        [SerializeField] private SettingUiControlHub settingUi;
        
        private new void Awake()
        {
            base.Awake();
            settingUi.OnSettingUiHide += SettingSave;
            AStarEventBus.Subscribe<SettingUiEvent>(InteractSetting);
        }
        private void OnDestroy()
        {
            settingUi.OnSettingUiHide -= SettingSave;
            AStarEventBus.Unsubscribe<SettingUiEvent>(InteractSetting);
        }
        
        public void InteractSetting(SettingUiEvent @event)
        {
            Debug.Log("진입 22");
            AStarEventBus.Publish(new UiInteractEvent(settingUi));
        }

        private void SettingSave(float masterVolume, float themeVolume, float sfxVolume)
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("ThemeVolume", themeVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        }
    }
}
