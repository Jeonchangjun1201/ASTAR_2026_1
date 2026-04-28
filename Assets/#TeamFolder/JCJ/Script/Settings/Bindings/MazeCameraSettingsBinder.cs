using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// SettingsService의 카메라 옵션을 미로 카메라 리그와 플레이어 컨트롤러에 적용한다.
    /// </summary>
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

            // 카메라 피치 잠금은 리그가 담당하고, 감도/반전/회전 방식은 각 플레이어 컨트롤러가 담당한다.
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
                controller.SetInvertY(data.invertY);
                controller.SetRotateMode(data.playerRotateMode);
            }
        }
    }
}
