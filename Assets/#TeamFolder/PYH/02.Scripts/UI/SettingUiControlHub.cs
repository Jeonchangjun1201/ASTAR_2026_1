using System;
using System.Collections.Generic;
using _TeamFolder.PYH._02.Scripts.Enum;
using _TeamFolder.PYH._02.Scripts.UI.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class SettingUiControlHub : PopupUi
    {
        public event Action<float, float, float> OnSettingVolumeSaveEvent;
        [SerializeField] private CanvasGroup popupCanvas;
        [SerializeField] private CanvasGroup settingCanvas;
        [SerializeField] private GameObject eventSystemPrefab;

        [SerializeField] private CanvasGroup soundPopup;
        [SerializeField] private CanvasGroup displayPopup;
        private SettingPopupEnum _curType;
        
        [SerializeField] private TMP_Text masterLabel;
        [SerializeField] private TMP_Text bgmLabel;
        [SerializeField] private TMP_Text sfxLabel;

        [SerializeField] private Scrollbar masterBar;
        [SerializeField] private Scrollbar bgmBar;
        [SerializeField] private Scrollbar sfxBar;
        
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        private readonly Vector2Int[] _resolutions =
        {
            new(1920, 1080),
            new(2560, 1440),
            new(3840, 2160),
            new(7680, 4320)
        };
        private int _resolutionIndex;
        
        // 슬라이더 UI 관련
        private float _masterAmount = 1f; // 0 ~ 1
        private float _bgmAmount = 0.5f; // 0 ~ 1
        private float _sfxAmount = 0.5f; // 0 ~ 1

        private void Awake()
        {
            if (EventSystem.current == null)
            {
                Instantiate(eventSystemPrefab);
            }
            
            _masterAmount = PlayerPrefs.GetFloat("MasterVolume", _masterAmount);
            _bgmAmount = PlayerPrefs.GetFloat("BGMVolume", _bgmAmount);
            _sfxAmount = PlayerPrefs.GetFloat("SFXVolume", _sfxAmount);

            SetSoundLabel(SoundLabelEnum.MASTER, _masterAmount);
            SetSoundLabel(SoundLabelEnum.BGM, _bgmAmount);
            SetSoundLabel(SoundLabelEnum.SFX, _sfxAmount);

            SetSoundBar(SoundLabelEnum.MASTER, _masterAmount);
            SetSoundBar(SoundLabelEnum.BGM, _bgmAmount);
            SetSoundBar(SoundLabelEnum.SFX, _sfxAmount);
            
            InitResolutionDropdown();
            LoadDisplaySetting();
            
            resolutionDropdown.onValueChanged.AddListener(SetResolutionIndex);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        private void InitResolutionDropdown()
        {
            resolutionDropdown.ClearOptions();

            List<string> options = new();

            foreach (var resolution in _resolutions)
                options.Add($"{resolution.x} X {resolution.y}");

            resolutionDropdown.AddOptions(options);
        }
        private void LoadDisplaySetting()
        {
            int width = PlayerPrefs.GetInt("ResolutionWidth", Screen.width);
            int height = PlayerPrefs.GetInt("ResolutionHeight", Screen.height);
            bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

            _resolutionIndex = FindResolutionIndex(width, height);

            resolutionDropdown.SetValueWithoutNotify(_resolutionIndex);
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

            ApplyResolution();
        }
        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < _resolutions.Length; i++)
            {
                if (_resolutions[i].x == width && _resolutions[i].y == height)
                    return i;
            }

            return 1; // 기본값: 1920 x 1080
        }
        public void SetResolutionIndex(int index)
        {
            _resolutionIndex = index;
            ApplyResolution();
        }
        public void SetFullscreen(bool isFullscreen)
        {
            ApplyResolution();
        }
        private void ApplyResolution()
        {
            Vector2Int resolution = _resolutions[_resolutionIndex];
            bool fullscreen = fullscreenToggle.isOn;

            Screen.SetResolution(
                resolution.x,
                resolution.y,
                fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed
            );

            PlayerPrefs.SetInt("ResolutionWidth", resolution.x);
            PlayerPrefs.SetInt("ResolutionHeight", resolution.y);
            PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        public override bool InteractPopup() // don't use on button => on click event
        {
            IsOpen = !IsOpen;
            
            popupCanvas.alpha = IsOpen ? 1 : 0;
            popupCanvas.interactable = IsOpen;
            popupCanvas.blocksRaycasts = IsOpen;
            
            settingCanvas.alpha = IsOpen ? 1 : 0;
            settingCanvas.interactable = IsOpen;
            settingCanvas.blocksRaycasts = IsOpen;

            if (!IsOpen)
            {
                OnSettingVolumeSaveEvent?.Invoke(_masterAmount, _bgmAmount, _sfxAmount);
            }
            
            return IsOpen;
        }

        public void OpenPopup(SettingPopupEnum popupType)
        {
            if (_curType == popupType) return;
            _curType = popupType;
            
            switch (popupType)
            {
                case SettingPopupEnum.SOUND:
                {
                    SetCanvas(soundPopup, true);
                    SetCanvas(displayPopup, false);
                    break;
                }

                case SettingPopupEnum.DISPLAY:
                {
                    SetCanvas(displayPopup, true);
                    SetCanvas(soundPopup, false);
                    break;
                }
            }
        }
        private void SetCanvas(CanvasGroup canvas, bool isOpen)
        {
            canvas.alpha = isOpen ? 1 : 0;
            canvas.blocksRaycasts = isOpen;
            canvas.interactable = isOpen;
        }

        public void SetSoundLabel(SoundLabelEnum labelType, float amount)
        {
            switch (labelType)
            {
                case SoundLabelEnum.MASTER:
                {
                    masterLabel.text = Math.Clamp((int)(amount * 100f), 0, 100).ToString() + '%';
                    break;
                }
                case SoundLabelEnum.BGM:
                {
                    bgmLabel.text = Math.Clamp((int)(amount * 100f), 0, 100).ToString() + '%';
                    break;
                }
                case SoundLabelEnum.SFX:
                {
                    sfxLabel.text = Math.Clamp((int)(amount * 100f), 0, 100).ToString() + '%';
                    break;
                }
            }
        }
        private void SetSoundBar(SoundLabelEnum labelType, float amount)
        {
            switch (labelType)
            {
                case SoundLabelEnum.MASTER:
                {
                    masterBar.value = amount;
                    break;
                }
                case SoundLabelEnum.BGM:
                {
                    bgmBar.value = amount;
                    break;
                }
                case SoundLabelEnum.SFX:
                {
                    sfxBar.value = amount;
                    break;
                }
            }
        }
    }
}
