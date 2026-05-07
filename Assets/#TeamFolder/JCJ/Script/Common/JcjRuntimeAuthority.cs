using System;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public enum JcjAuthorityMode
    {
        LocalSimulation = 0,
        ServerAuthoritative = 1
    }

    public sealed class JcjRuntimeAuthority : MonoBehaviour
    {
        [SerializeField] private JcjAuthorityMode _mode = JcjAuthorityMode.LocalSimulation;

        public static JcjRuntimeAuthority Instance { get; private set; }

        public static bool UseLocalSimulation =>
            Instance == null || Instance._mode == JcjAuthorityMode.LocalSimulation;

        public JcjAuthorityMode Mode => _mode;

        public event Action<JcjAuthorityMode> ModeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetMode(JcjAuthorityMode mode)
        {
            if (_mode == mode) return;
            _mode = mode;
            ModeChanged?.Invoke(_mode);
        }

        public static JcjRuntimeAuthority EnsureInstance()
        {
            if (Instance != null) return Instance;
            var existing = FindFirstObjectByType<JcjRuntimeAuthority>();
            if (existing != null) return existing;
            var root = new GameObject("[JCJ_RuntimeAuthority]");
            DontDestroyOnLoad(root);
            return root.AddComponent<JcjRuntimeAuthority>();
        }

        public static void SetServerAuthoritative(bool enabled)
        {
            EnsureInstance().SetMode(enabled ? JcjAuthorityMode.ServerAuthoritative : JcjAuthorityMode.LocalSimulation);
        }
    }
}
