using UnityEngine;
using _TeamFolder.JCJ.TileGame;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// SettingsService의 카메라 감도 값을 타일 게임 카메라에 전달한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class TileCameraSettingsBinder : MonoBehaviour
    {
        private ISettingsService _settings;

        private void Start()
        {
            _settings = SettingsService.EnsureInstance();
            _settings.OnChanged += HandleChanged;
            HandleChanged(_settings.Data);
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.OnChanged -= HandleChanged;
        }

        private void HandleChanged(SettingsData data)
        {
            if (data == null) return;
            // 타일 게임은 별도 카메라 팔로우 컴포넌트가 마우스 감도를 직접 보관한다.
            var camera = Object.FindFirstObjectByType<TileCameraFollow>();
            if (camera == null) return;
            camera.SetMouseSensitivity(data.cameraSensitivity);
        }
    }
}
