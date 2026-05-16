using System;
using UnityEngine;

// 설정 값을 저장하고 변경 이벤트를 전달하는 서비스.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 카메라, 미니맵, 키 설정을 PlayerPrefs에 저장하고 씬 전체에 변경 이벤트로 전달하는 설정 서비스.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class SettingsService : MonoBehaviour, ISettingsService
    {
        public const string PrefsKey = "JCJ.Settings.v1";

        public static SettingsService Instance { get; private set; }

        public SettingsData Data => _data;
        public event Action<SettingsData> OnChanged;

        private SettingsData _data = new SettingsData();
        private bool _loaded;

        public static SettingsService EnsureInstance()
        {
            // 설정 서비스는 씬을 넘어 유지되는 싱글톤이다.
            // Maze/Tile 어느 씬에서든 설정 UI와 플레이어가 같은 SettingsData를 참조하게 만든다.
            if (Instance != null) return Instance;
            var existing = FindFirstObjectByType<SettingsService>();
            if (existing != null) return existing;
            var go = new GameObject("[JCJ_SettingsService]");
            DontDestroyOnLoad(go);
            return go.AddComponent<SettingsService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Load()
        {
            // PlayerPrefs에 저장된 JSON 설정을 읽는다.
            // 잘못된 값이나 예전 버전 값은 SettingsData.ClampAndFix에서 안전한 기본값으로 보정된다.
            if (_loaded) return;
            _loaded = true;

            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<SettingsData>(json);
                    if (loaded != null)
                    {
                        _data = loaded;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SettingsService] Failed to parse saved settings, falling back to defaults. {e.Message}");
                    _data = new SettingsData();
                }
            }

            _data ??= new SettingsData();
            _data.ClampAndFix();
        }

        public void Save()
        {
            if (_data == null) return;
            _data.ClampAndFix();
            string json = JsonUtility.ToJson(_data);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
        }

        // 외부에서 완성된 설정 데이터를 한 번에 교체할 때 쓰는 진입점이다.
        // 서버 저장소를 붙이면 내려받은 설정 스냅샷을 이 메서드로 반영하면 된다.
        public void Apply(SettingsData updated, bool persist = true)
        {
            if (updated == null) return;
            _data = updated;
            _data.ClampAndFix();
            if (persist) Save();
            OnChanged?.Invoke(_data);
        }

        public void Mutate(Action<SettingsData> mutator, bool persist = true)
        {
            // 설정 UI가 값을 하나씩 바꿀 때 사용하는 진입점이다.
            // 변경 직후 저장하고 OnChanged를 쏴서 카메라/미니맵/키 바인딩 Binder들이 즉시 반영하게 한다.
            if (mutator == null || _data == null) return;
            mutator(_data);
            _data.ClampAndFix();
            if (persist) Save();
            OnChanged?.Invoke(_data);
        }

        public void ResetToDefaults()
        {
            _data = new SettingsData();
            Save();
            OnChanged?.Invoke(_data);
        }

        private void Start()
        {
            OnChanged?.Invoke(_data);
        }
    }
}
