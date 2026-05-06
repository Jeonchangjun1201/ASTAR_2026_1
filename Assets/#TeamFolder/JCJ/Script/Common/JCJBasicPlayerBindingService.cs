using System;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public enum JCJBasicPlayerBindingKey
    {
        MoveUp = 0,
        MoveDown = 1,
        MoveLeft = 2,
        MoveRight = 3,
        Jump = 4
    }

    [Serializable]
    public class JCJBasicPlayerBindingsData
    {
        public string moveUp = JCJBasicPlayerInputActions.DefaultMoveUp;
        public string moveDown = JCJBasicPlayerInputActions.DefaultMoveDown;
        public string moveLeft = JCJBasicPlayerInputActions.DefaultMoveLeft;
        public string moveRight = JCJBasicPlayerInputActions.DefaultMoveRight;
        public string jump = JCJBasicPlayerInputActions.DefaultJump;

        public JCJBasicPlayerBindingsData Clone()
        {
            return (JCJBasicPlayerBindingsData)MemberwiseClone();
        }

        public void ClampAndFix()
        {
            moveUp = Fallback(moveUp, JCJBasicPlayerInputActions.DefaultMoveUp);
            moveDown = Fallback(moveDown, JCJBasicPlayerInputActions.DefaultMoveDown);
            moveLeft = Fallback(moveLeft, JCJBasicPlayerInputActions.DefaultMoveLeft);
            moveRight = Fallback(moveRight, JCJBasicPlayerInputActions.DefaultMoveRight);
            jump = Fallback(jump, JCJBasicPlayerInputActions.DefaultJump);
        }

        public string GetBindingPath(JCJBasicPlayerBindingKey key)
        {
            return key switch
            {
                JCJBasicPlayerBindingKey.MoveUp => moveUp,
                JCJBasicPlayerBindingKey.MoveDown => moveDown,
                JCJBasicPlayerBindingKey.MoveLeft => moveLeft,
                JCJBasicPlayerBindingKey.MoveRight => moveRight,
                JCJBasicPlayerBindingKey.Jump => jump,
                _ => string.Empty
            };
        }

        public void SetBindingPath(JCJBasicPlayerBindingKey key, string path)
        {
            var safePath = Fallback(path, JCJBasicPlayerInputActions.GetDefaultPath(key));

            switch (key)
            {
                case JCJBasicPlayerBindingKey.MoveUp:
                    moveUp = safePath;
                    break;
                case JCJBasicPlayerBindingKey.MoveDown:
                    moveDown = safePath;
                    break;
                case JCJBasicPlayerBindingKey.MoveLeft:
                    moveLeft = safePath;
                    break;
                case JCJBasicPlayerBindingKey.MoveRight:
                    moveRight = safePath;
                    break;
                case JCJBasicPlayerBindingKey.Jump:
                    jump = safePath;
                    break;
            }
        }

        private static string Fallback(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }

    [DefaultExecutionOrder(-300)]
    public class JCJBasicPlayerBindingService : MonoBehaviour
    {
        public const string PrefsKey = "JCJ.BasicPlayerBindings.v1";

        public static JCJBasicPlayerBindingService Instance { get; private set; }

        public JCJBasicPlayerBindingsData Data => _data;
        public event Action<JCJBasicPlayerBindingsData> OnChanged;

        private JCJBasicPlayerBindingsData _data = new();
        private bool _loaded;

        public static JCJBasicPlayerBindingService EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindFirstObjectByType<JCJBasicPlayerBindingService>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("[JCJ_BasicPlayerBindingService]");
            DontDestroyOnLoad(go);
            return go.AddComponent<JCJBasicPlayerBindingService>();
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
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Load()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            var json = PlayerPrefs.GetString(PrefsKey, string.Empty);

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<JCJBasicPlayerBindingsData>(json);
                    if (loaded != null)
                    {
                        _data = loaded;
                    }
                }
                catch
                {
                    _data = new JCJBasicPlayerBindingsData();
                }
            }

            _data ??= new JCJBasicPlayerBindingsData();
            _data.ClampAndFix();
        }

        public void Save()
        {
            if (_data == null)
            {
                return;
            }

            _data.ClampAndFix();
            var json = JsonUtility.ToJson(_data);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
        }

        public string GetBindingPath(JCJBasicPlayerBindingKey key)
        {
            _data ??= new JCJBasicPlayerBindingsData();
            _data.ClampAndFix();
            return _data.GetBindingPath(key);
        }

        public void SetBindingPath(JCJBasicPlayerBindingKey key, string path, bool persist = true)
        {
            _data ??= new JCJBasicPlayerBindingsData();
            _data.SetBindingPath(key, path);
            _data.ClampAndFix();

            if (persist)
            {
                Save();
            }

            OnChanged?.Invoke(_data);
        }

        public void ResetToDefaults(bool persist = true)
        {
            _data = new JCJBasicPlayerBindingsData();
            _data.ClampAndFix();

            if (persist)
            {
                Save();
            }

            OnChanged?.Invoke(_data);
        }

        private void Start()
        {
            OnChanged?.Invoke(_data);
        }
    }
}
