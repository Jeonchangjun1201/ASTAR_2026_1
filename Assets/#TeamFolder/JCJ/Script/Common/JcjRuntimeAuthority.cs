using System; // 이벤트(Action 등)와 기본 형식을 쓰기 위해 CLR 네임스페이스를 포함한다.
using UnityEngine; // MonoBehaviour, GameObject, Destroy 등 유니티 런타임 API를 쓴다.

namespace _TeamFolder.JCJ.Script // JCJ 공통 스크립트 이름 공간으로 묶어 다른 폴더 코드와 충돌을 줄인다.
{
    public enum JcjAuthorityMode // 이 프로젝트가 지금 로컬에서만 판정하는지 서버가 권한을 갖는지 구분하는 열거형이다.
    {
        LocalSimulation = 0, // 한 클라이언트 안에서만 시뮬레이션하며 SpawnPlayers 같은 로컬 전용 분기가 활성화된다.
        ServerAuthoritative = 1 // 히트·리스폰·매치 구성 등 최종 판정을 서버가 내리고 클라는 적용만 한다는 의미로 사용된다.
    }

    public sealed class JcjRuntimeAuthority : MonoBehaviour // 씬에 하나 두고 DontDestroyOnLoad로 살려 전역 권한 플래그를 제공한다.
    {
        [SerializeField] private JcjAuthorityMode _mode = JcjAuthorityMode.LocalSimulation; // 인스펙터 기본값. 배포 빌드에서도 에디터로 바꿀 수 있다.

        public static JcjRuntimeAuthority Instance { get; private set; } // 싱글톤 참조. 없으면 UseLocalSimulation이 true로 떨어지도록 설계됐다.

        public static bool UseLocalSimulation => // 다른 스크립트가 if (UseLocalSimulation) 로 로컬/서버 분기할 때 읽는다.
            Instance == null || Instance._mode == JcjAuthorityMode.LocalSimulation; // 인스턴스가 없으면 안전하게 로컬 시뮬로 간주한다.

        public JcjAuthorityMode Mode => _mode; // 현재 모드를 외부에서 읽기만 할 때 쓴다.

        public event Action<JcjAuthorityMode> ModeChanged; // 네트워크 레이어가 모드 전환 때 구독해 초기화할 수 있다.

        private void Awake() // 오브젝트 생성 직후 싱글톤을 등록하고 중복 컴포넌트를 제거한다.
        {
            if (Instance != null && Instance != this) // 이미 다른 오브젝트가 싱글톤 역할을 하면 자신은 파괴된다.
            {
                Destroy(gameObject); // 중복 권한 오브젝트 전체를 없애 혼선을 막는다.
                return; // 아래 등록 로직을 실행하지 않는다.
            }

            Instance = this; // 유일한 싱글톤으로 자신을 등록한다.
            DontDestroyOnLoad(gameObject); // 씬 전환 후에도 모드 값을 유지한다.
        }

        private void OnDestroy() // 오브젝트 파괴 시 전역 참조를 비워 댕글링을 줄인다.
        {
            if (Instance == this) Instance = null; // 내가 등록한 싱글톤일 때만 null로 만든다.
        }

        public void SetMode(JcjAuthorityMode mode) // 런타임에 로컬/서버 권한 전환을 넣을 때 호출한다.
        {
            if (_mode == mode) return; // 같은 값이면 이벤트 스팸을 막는다.
            _mode = mode; // 내부 상태를 갱신한다.
            ModeChanged?.Invoke(_mode); // 구독자에게 새 모드를 알린다.
        }

        public static JcjRuntimeAuthority EnsureInstance() // 씬에 없으면 빈 게임오브젝트를 만들어 붙인다.
        {
            if (Instance != null) return Instance; // 이미 살아 있으면 그대로 반환한다.
            var existing = FindFirstObjectByType<JcjRuntimeAuthority>(); // 씬 어딘가에 숨어 있는 컴포넌트를 찾는다.
            if (existing != null) return existing; // 찾았으면 새로 만들지 않는다.
            var root = new GameObject("[JCJ_RuntimeAuthority]"); // 계층 창에서 찾기 쉬운 이름의 루트를 만든다.
            DontDestroyOnLoad(root); // 새로 만든 루트도 씬 전환 시 유지한다.
            return root.AddComponent<JcjRuntimeAuthority>(); // 컴포넌트를 붙여 Awake가 Instance를 채우게 한다.
        }

        public static void SetServerAuthoritative(bool enabled) // 서버 개발자가 한 줄로 서버 권한 모드를 켜고 끌 때 쓴다.
        {
            EnsureInstance().SetMode(enabled ? JcjAuthorityMode.ServerAuthoritative : JcjAuthorityMode.LocalSimulation); // 인스턴스를 보장한 뒤 열거값으로 설정한다.
        }
    }
}
