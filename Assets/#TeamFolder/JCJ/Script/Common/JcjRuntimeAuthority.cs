using System;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// JCJ 클라이언트가 로컬 시뮬레이션인지 서버 권한 모드인지 구분한다.
    /// 서버 연동 시 빌드/런타임에 <see cref="SetServerAuthoritative"/> 또는 인스펙터 Mode를 ServerAuthoritative로 설정한다.
    /// 각 게이트웨이는 <see cref="UseLocalSimulation"/>이 false일 때 *Requested 이벤트만 발생시키고,
    /// 서버가 확정한 뒤 ApplyAuthoritative* 메서드로 상태를 반영한다.
    /// </summary>
    public enum JcjAuthorityMode
    {
        LocalSimulation = 0,
        ServerAuthoritative = 1
    }

    public sealed class JcjRuntimeAuthority : MonoBehaviour
    {
        [SerializeField] private JcjAuthorityMode _mode = JcjAuthorityMode.LocalSimulation;

        public static JcjRuntimeAuthority Instance { get; private set; }

        /// <summary>인스턴스가 없으면 로컬 시뮬로 간주(오프라인·에디터 안전 기본값).</summary>
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

        /// <summary>서버 붙일 때 한 줄로 권한 모드 전환.</summary>
        public static void SetServerAuthoritative(bool enabled) =>
            EnsureInstance().SetMode(enabled ? JcjAuthorityMode.ServerAuthoritative : JcjAuthorityMode.LocalSimulation);
    }
}
