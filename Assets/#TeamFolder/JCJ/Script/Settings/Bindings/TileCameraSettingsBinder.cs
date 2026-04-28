using UnityEngine;
using _TeamFolder.JCJ.TileGame;

namespace _TeamFolder.JCJ.Script
{
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
            var camera = Object.FindFirstObjectByType<TileCameraFollow>();
            if (camera == null) return;
            camera.SetMouseSensitivity(data.cameraSensitivity);
            camera.SetAllowPitch(!data.lockPitch);
        }
    }
}
