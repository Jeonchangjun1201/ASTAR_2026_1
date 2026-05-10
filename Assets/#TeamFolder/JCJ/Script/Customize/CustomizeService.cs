using System;
using UnityEngine;

// 외형 설정 저장과 변경 이벤트를 관리하는 서비스.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 플레이어 색상 커스터마이즈 값을 저장하고 생성된 캐릭터 비주얼에 적용하는 서비스.
    /// </summary>
    [DefaultExecutionOrder(-180)]
    public class CustomizeService : MonoBehaviour, ICustomizeService
    {
        public const string PrefsKey = "JCJ.Customize.v1";

        public static CustomizeService Instance { get; private set; }
        public CustomizeData Data => _data;
        public event Action<CustomizeData> OnChanged;

        private CustomizeData _data = new CustomizeData();
        private bool _loaded;

        public static CustomizeService EnsureInstance()
        {
            // 씬마다 따로 만들지 않고 DontDestroyOnLoad 싱글톤으로 커스터마이즈 상태를 공유한다.
            if (Instance != null) return Instance;
            var existing = FindFirstObjectByType<CustomizeService>();
            if (existing != null) return existing;
            var go = new GameObject("[JCJ_CustomizeService]");
            DontDestroyOnLoad(go);
            return go.AddComponent<CustomizeService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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
            // 저장된 JSON이 없거나 파싱에 실패하면 기본 외형으로 시작한다.
            if (_loaded) return;
            _loaded = true;
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<CustomizeData>(json);
                    if (loaded != null) _data = loaded;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CustomizeService] Failed to parse, using defaults. {e.Message}");
                    _data = new CustomizeData();
                }
            }
            _data ??= new CustomizeData();
            _data.ClampAndFix();
        }

        public void Save()
        {
            if (_data == null) return;
            _data.ClampAndFix();
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(_data));
            PlayerPrefs.Save();
        }

        public void Apply(CustomizeData updated, bool persist = true)
        {
            if (updated == null) return;
            _data = updated;
            _data.ClampAndFix();
            if (persist) Save();
            OnChanged?.Invoke(_data);
        }

        public void Mutate(Action<CustomizeData> mutator, bool persist = true)
        {
            if (mutator == null || _data == null) return;
            mutator(_data);
            _data.ClampAndFix();
            if (persist) Save();
            OnChanged?.Invoke(_data);
        }

        public void ResetToDefaults()
        {
            _data = new CustomizeData();
            Save();
            OnChanged?.Invoke(_data);
        }

        public void ApplyTo(GameObject characterRoot)
        {
            // 캐릭터 프리팹 하위의 모든 PartyCharacterVisual에 현재 색상 설정을 밀어 넣는다.
            if (characterRoot == null || _data == null) return;
            var visuals = characterRoot.GetComponentsInChildren<PartyCharacterVisual>(true);
            foreach (var v in visuals)
            {
                if (v == null) continue;
                v.ApplyCustomization(_data);
            }
        }

        private void Start()
        {
            OnChanged?.Invoke(_data);
        }
    }
}
