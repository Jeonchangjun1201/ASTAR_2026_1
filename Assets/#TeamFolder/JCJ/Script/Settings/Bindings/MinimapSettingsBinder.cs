using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// SettingsService의 미니맵 색상, 위치, 크기 값을 현재 미로 미니맵 UI에 반영한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class MinimapSettingsBinder : MonoBehaviour
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
            var minimap = Object.FindFirstObjectByType<MazeMinimap>();
            if (minimap == null) return;

            // 미니맵 프리셋은 SettingsData에서 앵커와 여백 좌표로 변환한다.
            minimap.SetPlayerColor(data.minimapPlayerColor);
            minimap.SetAnchor(data.GetMinimapAnchor(), data.GetMinimapAnchoredPos());
            minimap.SetSize(new Vector2(data.minimapSize, data.minimapSize));
        }
    }
}
