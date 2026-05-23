using System;
using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using _TeamFolder.PYH._02.Scripts.UI.Scene;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class SettingUiControlHub : PopupUi
    {
        public event Action<float, float, float> OnSettingUiHide;
        [SerializeField] private CanvasGroup canvas;
        
        //[SerializeField] private 슬라이더 UI 관련
        private float _masterAmount = 50; // 0 ~ 100
        private float _themeAmount = 50; // 0 ~ 100
        private float _sfxAmount = 50; // 0 ~ 100
        
        private void Awake()
        {
            _masterAmount = PlayerPrefs.GetFloat("MasterVolume", _masterAmount);
            _themeAmount = PlayerPrefs.GetFloat("ThemeVolume", _themeAmount);
            _sfxAmount = PlayerPrefs.GetFloat("SFXVolume", _sfxAmount);
        }

        public override void InteractPopup()
        {
            Debug.Log(IsOpen + "1");
            IsOpen = !IsOpen;
            Debug.Log(IsOpen + "2");
            
            canvas.alpha = IsOpen ? 1 : 0;
            canvas.interactable = IsOpen;
            canvas.blocksRaycasts = IsOpen;

            if (IsOpen) return;
            
            OnSettingUiHide?.Invoke(_masterAmount, _themeAmount, _sfxAmount);
            AStarEventBus.Publish(new UiInteractEvent(this));
        }
    }
}
