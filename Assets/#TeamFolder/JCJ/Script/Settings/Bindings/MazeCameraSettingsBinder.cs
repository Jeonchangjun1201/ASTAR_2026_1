using UnityEngine;

// 미로 카메라 설정을 실제 카메라 컴포넌트에 반영하는 바인더.

namespace _TeamFolder.JCJ.Script
{
    [DisallowMultipleComponent]
    public class MazeCameraSettingsBinder : MonoBehaviour
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

            var rig = MazeCameraRig.Instance;
            if (rig != null)
            {
                rig.SetAllowPitch(!data.lockPitch);
            }

            ApplyToAllControllers(data);
        }

        private static void ApplyToAllControllers(SettingsData data)
        {
            var all = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var controller in all)
            {
                if (controller == null) continue;
                controller.SetMouseSensitivity(data.cameraSensitivity);
            }
        }
    }
}
