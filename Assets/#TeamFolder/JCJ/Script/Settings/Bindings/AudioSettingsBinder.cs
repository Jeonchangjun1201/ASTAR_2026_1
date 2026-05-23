using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// SettingsData 볼륨 변경을 Maze/Tile 오디오 등에 즉시 반영한다.
    /// SettingsService와 같은 오브젝트에 붙이거나 EnsureInstance 시 자동 추가된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioSettingsBinder : MonoBehaviour
    {
        private ISettingsService _settings;

        private void Awake()
        {
            _settings = GetComponent<ISettingsService>();
            if (_settings == null)
                _settings = SettingsService.EnsureInstance();
        }

        private void OnEnable()
        {
            if (_settings != null)
                _settings.OnChanged += HandleChanged;
            HandleChanged(_settings?.Data);
        }

        private void OnDisable()
        {
            if (_settings != null)
                _settings.OnChanged -= HandleChanged;
        }

        private void HandleChanged(SettingsData data)
        {
            ApplyToSceneAudio();
        }

        public static void ApplyToSceneAudio()
        {
            JcjSoundPlayback.SyncSoundManagerMaster();
            JcjFootstepAudio.RefreshVolumeIfWalking();

            var mazeAudios = Object.FindObjectsByType<MazeAudio>(FindObjectsSortMode.None);
            for (int i = 0; i < mazeAudios.Length; i++)
            {
                if (mazeAudios[i] != null)
                    mazeAudios[i].ApplyUserVolumeSettings();
            }

            var tileAudios = Object.FindObjectsByType<TileGame.TileAudio>(FindObjectsSortMode.None);
            for (int i = 0; i < tileAudios.Length; i++)
            {
                if (tileAudios[i] != null)
                    tileAudios[i].ApplyUserVolumeSettings();
            }
        }
    }
}
