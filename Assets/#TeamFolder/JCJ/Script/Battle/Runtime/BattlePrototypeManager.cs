using System.Collections; // 코루틴 IEnumerator를 사용한다.
using System.Collections.Generic; // List 등 컬렉션을 사용한다.
using System.Text; // 리더보드 문자열 조립에 StringBuilder를 쓴다.
using UnityEngine; // MonoBehaviour와 물리, Instantiate 등 유니티 API이다.
using _TeamFolder.JCJ.Script; // JcjRuntimeAuthority와 RuntimePlayerIdentity를 포함한다.
using _TeamFolder.JCJ.Battle.Session; // IBattleMatchGateway 등 세션 계약이다.

// BattlePrototypeScene 중심 오케스트레이터. 전투 프로토타입의 라운드, 스폰, 팀, 점수를 총괄한다.
//
// 서버 연동 관점 요약
// - 장점: JcjRuntimeAuthority가 ServerAuthoritative면 Start에서 로컬 스폰·연출을 건너뛰고 MatchSetupRequested만 발생시킨다.
//   이후 네트워크 레이어가 ApplyAuthoritativeMatchSetup / ApplyAuthoritativeRespawn / StartMatchPresentation으로 씬 상태를 맞추면 된다.
// - BattleMatchRegistry에 등록되므로 다른 스크립트는 BattleMatchRegistry.Match로 IBattleMatchGateway에 접근할 수 있다.
// - 보완 예정으로 두기 좋은 부분: SpawnPlayers의 Instantiate를 네트워크 스폰으로 치환, 무기는 WeaponId 문자열 동기화 후 EquipWeapon,
//   킬/데스는 HandlePlayerDeath의 서버 분기처럼 서버 확정 이벤트만 반영하는 단일 경로로 통일.

namespace _TeamFolder.JCJ.Battle // 배틀 모드 전용 코드 네임스페이스다.
{
    /// <summary>배틀 프로토타입 씬의 단일 매니저. <see cref="BattleMatchRegistry"/> 등록·<see cref="IBattleMatchGateway"/> 구현으로 서버 레이어와 연결한다.</summary>
    public class BattlePrototypeManager : MonoBehaviour, IBattleMatchGateway, IBattlePopupPresentation // 배틀 씬 단일 진입점이며 게이트웨이와 팝업 설정을 동시에 구현한다.
    {
        private sealed class PlayerSlot // 한 명분 컴포넌트와 통계를 한 줄 구조체에 모아 둔다.
        {
            public GameObject Instance; // 씬에 존재하는 플레이어 루트 오브젝트이다.
            public PlayerController Controller; // 이동 및 입력 컨트롤러이다.
            public BattleHealth Health; // 데미지와 사망 이벤트이다.
            public BattleWeaponManager WeaponManager; // 무기 장착과 발사 입력이다.
            public int Rank; // 무기 등급 계산에 쓰는 순위 값이다.
            public int TeamIndex; // 0 또는 1 팀 소속이다.
            public bool IsLocal; // 로컬 입력 소유자 여부이다.
            public BattleWeaponDefinition SelectedWeapon; // 현재 장착된 무기 정의이다.
            public int Kills; // 로컬 시뮬에서만 증가하는 킬 수이다.
            public int Deaths; // 로컬 시뮬에서만 증가하는 데스 수이다.
        }

        [SerializeField] private GameObject _playerPrefab; // Instantiate 할 플레이어 바디 프리팹이다.
        [SerializeField] private BattleWeaponCatalog _weaponCatalog; // 랭크별 무기 목록 데이터 소스이다.
        [SerializeField] private BattleFirstPersonCamera _battleCamera; // 로컬 플레이어 카메라 참조이다.
        [SerializeField] private ScoreService _scoreService;
        [SerializeField] private GameObject _matchScoreRankPrefab;
        [SerializeField] private GameObject _scoreServicePrefab;
        [SerializeField] private bool _useScoreServiceRanks = true; // 점수 서비스에서 랭크 배열을 덮어쓸지 여부이다.
        [SerializeField] private Transform _spawnRoot; // 스폰 포인트 자식들의 부모 트랜스폼이다.
        [SerializeField] private int _localPlayerIndex; // 로컬 플레이어 슬롯 인덱스이다.
        [SerializeField] private int[] _playerRanks = { 2, 3, 1, 4 }; // 각 슬롯의 시드 랭크 배열이다.
        [SerializeField] private int[] _playerTeamIndices = { 0, 1, 0, 1 }; // 0 또는 1 팀 번호 배열이다.
        [SerializeField] private Color _teamOneColor = new(0.22f, 0.72f, 1f, 1f); // 첫 번째 팀 색이다.
        [SerializeField] private Color _teamTwoColor = new(1f, 0.36f, 0.36f, 1f); // 두 번째 팀 색이다.
        [SerializeField] private string _teamOneName = "BLUE TEAM"; // UI에 표시할 첫 팀 이름이다.
        [SerializeField] private string _teamTwoName = "RED TEAM"; // UI에 표시할 둘째 팀 이름이다.
        [SerializeField] private float _weaponRollDuration = 1.2f; // 무기 뽑기 연출 시간이다.
        [SerializeField] private float _weaponRevealDuration = 1f; // 최종 무기 공개 후 대기 시간이다.
        [SerializeField] private AudioClip _weaponRollSfx; // 무기 뽑기 룰렛 루프 SFX (기본: roll.wav).
        [SerializeField] [Range(0f, 1f)] private float _weaponRollSfxVolume = 0.9f;
        [SerializeField] private AudioClip _weaponRevealSfx; // 무기 확정 SFX (기본: wow.mp3).
        [SerializeField] [Range(0f, 1f)] private float _weaponRevealSfxVolume = 1f;
        [SerializeField] private float _countdownStepDuration = 1f; // 카운트다운 한 숫자당 초 단위이다.
        [SerializeField] private int _countdownStart = 3; // 카운트다운 시작 숫자이다.
        [SerializeField] [Range(0f, 1f)] private float _countdownSfxVolume = 0.9f;
        [SerializeField] private float _respawnDelay = 3f; // 로컬 시뮬 리스폰 대기 시간이다.
        [SerializeField] private float _spawnProtectionDuration = 2f; // 리스폰 직후 무적 시간이다.
        [SerializeField] private float _arenaBoundaryPadding = 1.4f; // 낙사 복구 경계 패딩이다.
        [SerializeField] private float _arenaFallThresholdY = -6f; // 이 높이 이하면 추락으로 간주한다.
        [SerializeField] private float _arenaRecoverHeight = 1f; // 복구 시 위로 올리는 높이이다.
        [SerializeField] private float _headLocalY = 0.72f; // 머리 콜라이더 로컬 Y 위치이다.
        [SerializeField] private float _headRadius = 0.65f; // 머리 구체 반경이다.
        [SerializeField] private float _popupScale = 0.005f; // 데미지 팝업 월드 스케일이다.
        [SerializeField] private int _popupFontSize = 130; // 일반 팝업 폰트 크기이다.
        [SerializeField] private int _popupHeadshotFontSize = 180; // 헤드샷 팝업 폰트 크기이다.
        [SerializeField] private bool _battleUseThirdPersonCamera; // true면 3인칭 카메라 모드이다.
        [SerializeField] private bool _battleDisableJumpInThisScene; // true면 배틀에서 점프를 막는다.

        private readonly List<GameObject> _players = new(); // 생성된 플레이어 오브젝트 목록이다.
        private readonly List<PlayerSlot> _playerSlots = new(); // 각 플레이어의 컴포넌트 묶음이다.

        private static BattlePrototypeManager _instance; // 정적 접근용 캐시이며 필수는 아니다.
        private Vector3 _originalGravity; // 씬 진입 전 물리 중력 값 보관이다.
        private BattleIntroUI _introUI; // 카운트다운 등 연출 UI이다.
        private BattleLeaderboardUI _leaderboardUI; // 점수판 UI이다.
        private Transform _arenaRoot; // 경계 연출의 기준 트랜스폼이다.
        private int[] _teamTargetScores = new int[2]; // 팀별 목표 점수이다.
        private int[] _teamCurrentScores = new int[2]; // 팀별 현재 점수이다.
        private bool _matchEnded; // 승리 처리 후 true이다.
        private bool _matchPresentationStarted; // 인트로 코루틴 중복 방지 플래그이다.

        private static AudioClip _cachedCountdownTickClip;
        private static AudioClip _cachedCountdownGoClip;

        public event System.Action MatchSetupRequested; // 서버 권한 모드에서 Start가 여기까지 실행된 뒤 한 번 올려 네트워크 초기화를 트리거한다.

        public event System.Action<string> RespawnRequested; // 서버 권한 데스에서 피해자 Id만 알리고 실제 좌표 적용은 별도 RPC에서 한다.

        float IBattlePopupPresentation.DamagePopupWorldScale => _popupScale; // 인터페이스 명시 구현으로 외부에서 팝업 크기를 읽는다.
        int IBattlePopupPresentation.DamagePopupFontSize => _popupFontSize; // 일반 데미지 글자 크기이다.
        int IBattlePopupPresentation.DamagePopupHeadshotFontSize => _popupHeadshotFontSize; // 헤드샷 글자 크기이다.

        public static float PopupScale =>
            BattleMatchRegistry.Popups?.DamagePopupWorldScale ?? BattleMatchRegistry.DefaultDamagePopupWorldScale; // 레지스트리 우선, 없으면 상수 폴백이다.
        public static int PopupFontSize =>
            BattleMatchRegistry.Popups?.DamagePopupFontSize ?? BattleMatchRegistry.DefaultDamagePopupFontSize; // 동일 패턴이다.
        public static int PopupHeadshotFontSize =>
            BattleMatchRegistry.Popups?.DamagePopupHeadshotFontSize ?? BattleMatchRegistry.DefaultDamagePopupHeadshotFontSize; // 동일 패턴이다.

        /// <summary>씬 시작 1회. 로컬 시뮬이면 스폰~리더보드~연출, 서버 권한이면 레지스트리 등록 후 <see cref="MatchSetupRequested"/>만 올린다.</summary>
        private void Start()
        {
            _instance = this; // 정적 조회용으로 자신을 저장한다.
            BattleMatchRegistry.Register(this); // 서버 코드가 BattleMatchRegistry.Match로 찾을 수 있게 등록한다.
            _originalGravity = Physics.gravity; // 나중에 OnDestroy에서 되돌리기 위해 보관한다.
            Physics.gravity = new Vector3(0f, -25f, 0f); // 배틀 씬 전용 중력으로 바꾼다.

            if (_playerPrefab == null || _weaponCatalog == null) return; // 필수 에셋이 비면 초기화를 중단한다.

            if (_battleCamera == null) _battleCamera = Object.FindFirstObjectByType<BattleFirstPersonCamera>(); // 인스펙터 미연결이면 씬에서 탐색한다.
            if (_battleCamera != null) _battleCamera.SetThirdPersonMode(_battleUseThirdPersonCamera); // 카메라 모드 플래그를 적용한다.
            JcjAudioAmbience.EnsureAudioListener();
            ResolveScoreService(); // ScoreService 참조를 확보한다.
            ApplyRanksFromScoreService(); // 외부 점수 서비스에서 랭크를 끌어온다.
            RandomizeTeamAssignments(); // 로컬 시뮬일 때만 의미 있는 팀 셔플이다.
            ResolveArenaRoot(); // 경계 연출용 아레나 루트를 찾는다.
            EnsureSpawnRoot(); // 스폰 포인트 부모가 없으면 만든다.
            EnsureIntroUI(); // 인트로 UI 컴포넌트를 보장한다.
            EnsureLeaderboardUI(); // 리더보드 UI 컴포넌트를 보장한다.
            if (!JcjRuntimeAuthority.UseLocalSimulation) // 서버 권한 모드 분기이다.
            {
                MatchSetupRequested?.Invoke(); // 여기서 멈추고 네트워크 레이어가 이후 슬롯 채움을 담당한다.
                return; // 로컬 SpawnPlayers와 연출 시작을 실행하지 않는다.
            }
            SpawnPlayers(); // 로컬에서는 즉시 플레이어 오브젝트를 만든다.
            ApplySavedPlayerSettings(); // 런타임 스폰 직후 저장된 DPI·키 설정을 반영한다.
            CalculateTeamTargets(); // 팀 목표 점수를 계산한다.
            RefreshLeaderboard(); // UI를 첫 동기화한다.
            StartMatchPresentation(); // 인트로 코루틴을 시작한다.
        }

        /// <summary>무기 연출·카운트다운 코루틴 시작. 서버에서 슬롯 채운 뒤 <see cref="ApplyAuthoritativeMatchSetup"/>에서 최초 1회 호출되는 흐름이 일반적이다.</summary>
        public void StartMatchPresentation()
        {
            if (_matchPresentationStarted) return; // 이미 시작했으면 무시한다.
            _matchPresentationStarted = true; // 플래그를 올려 중복 실행을 막는다.
            StartCoroutine(BeginMatchRoutine()); // 실제 연출 시퀀스 코루틴을 돌린다.
        }

        /// <summary>중력 복구 및 <see cref="BattleMatchRegistry.Unregister"/>로 전역 참조 해제.</summary>
        private void OnDestroy()
        {
            EndWeaponRollSfx();
            Physics.gravity = _originalGravity; // 전역 중력 설정을 원래 값으로 돌린다.
            if (_instance == this)
            {
                BattleMatchRegistry.Unregister(this); // 전역 레지스트리에서 자신을 제거한다.
                _instance = null; // 정적 참조를 비운다.
            }
        }

        /// <summary>에디터에서 HeadHitbox 구체 범위를 시각화한다.</summary>
        private void OnDrawGizmos()
        {
            if (_players == null || _players.Count == 0) return;
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            foreach (var player in _players)
            {
                if (player == null) continue;
                var head = player.transform.Find("HeadHitbox");
                if (head == null) continue;
                var sphere = head.GetComponent<SphereCollider>();
                if (sphere == null) continue;
                float worldRadius = sphere.radius * player.transform.lossyScale.x;
                Gizmos.DrawWireSphere(head.position, worldRadius);
                Gizmos.DrawSphere(head.position, worldRadius * 0.3f);
            }
        }

        /// <summary>같은 오브젝트에 <see cref="BattleIntroUI"/>가 없으면 추가한다.</summary>
        private void EnsureIntroUI()
        {
            _introUI = GetComponent<BattleIntroUI>();
            if (_introUI == null) _introUI = gameObject.AddComponent<BattleIntroUI>();
        }

        /// <summary>같은 오브젝트에 <see cref="BattleLeaderboardUI"/>가 없으면 추가한다.</summary>
        private void EnsureLeaderboardUI()
        {
            _leaderboardUI = GetComponent<BattleLeaderboardUI>();
            if (_leaderboardUI == null) _leaderboardUI = gameObject.AddComponent<BattleLeaderboardUI>();
        }

        /// <summary>씬의 DeathmatchArena 오브젝트로 아레나 루트를 찾는다. 낙사 복구 등에 사용.</summary>
        private void ResolveArenaRoot()
        {
            if (_arenaRoot != null) return;
            var arenaObject = GameObject.Find("DeathmatchArena");
            if (arenaObject != null) _arenaRoot = arenaObject.transform;
        }

        // 시작 연출, 팀 표시, 무기 지급, 입력 허용 순서를 묶는 코루틴.
        // 서버 연동 시 권장: 서버가 "매치 시작 시각" 또는 단계 enum을 브로드캐스트하고, 클라는 그에 맞춰 카운트다운·입력 해제만 수행.
        // 무기 최종값은 서버가 준 WeaponId로 EquipWeapon하는 경로로 바꾸면 PlayLocalWeaponDraw의 Random 최종무기와 충돌하지 않는다.
        /// <summary>팀 색 적용, 원거리 무기 장착, 로컬 무기 뽑기·카운트다운 후 <see cref="EnableGameplay"/>.</summary>
        private IEnumerator BeginMatchRoutine()
        {
            yield return null;

            ApplyTeamPresentation();
            EquipRemoteWeapons();

            var localSlot = GetLocalSlot();
            if (localSlot != null)
            {
                yield return StartCoroutine(PlayLocalWeaponDraw(localSlot));
                for (int count = Mathf.Max(1, _countdownStart); count > 0; count--)
                {
                    _introUI.ShowCountdown(GetTeamName(localSlot.TeamIndex), GetTeamColor(localSlot.TeamIndex), count);
                    PlayCountdownTickSfx();
                    yield return new WaitForSeconds(_countdownStepDuration);
                }

                _introUI.ShowCountdown(GetTeamName(localSlot.TeamIndex), GetTeamColor(localSlot.TeamIndex), 0);
                PlayCountdownGoSfx();
                EnableGameplay();
                yield return new WaitForSeconds(0.35f);
                _introUI.Hide();
            }
            else
            {
                EnableGameplay();
            }
        }

        // 로컬 플레이어 전용 연출. 서버 게임에서는 (1) 서버 확정 WeaponId만 반영하거나 (2) 연출용 RNG는 서버 시드로 맞추는 편이 안전하다.
        /// <summary>로컬 슬롯만 무기 랜덤 롤 연출 후 최종 장착. <see cref="BeginMatchRoutine"/>에서만 호출된다.</summary>
        private IEnumerator PlayLocalWeaponDraw(PlayerSlot slot)
        {
            var candidates = GetWeaponCandidates(slot.Rank);
            if (candidates.Length == 0) yield break;

            var finalWeapon = candidates[Random.Range(0, candidates.Length)];
            float endTime = Time.time + Mathf.Max(0.1f, _weaponRollDuration);
            int lastIndex = -1;

            BeginWeaponRollSfx();
            while (Time.time < endTime)
            {
                int nextIndex = candidates.Length > 1 ? Random.Range(0, candidates.Length) : 0;
                if (candidates.Length > 1 && nextIndex == lastIndex) nextIndex = (nextIndex + 1) % candidates.Length;
                lastIndex = nextIndex;
                var rollingWeapon = candidates[nextIndex];
                slot.WeaponManager.EquipWeapon(rollingWeapon);
                _introUI.ShowWeaponRoll(GetTeamName(slot.TeamIndex), GetTeamColor(slot.TeamIndex), slot.Rank, rollingWeapon, false);
                yield return new WaitForSeconds(0.08f);
            }

            EndWeaponRollSfx();

            slot.SelectedWeapon = finalWeapon;
            slot.WeaponManager.EquipWeapon(finalWeapon);
            _introUI.ShowWeaponRoll(GetTeamName(slot.TeamIndex), GetTeamColor(slot.TeamIndex), slot.Rank, finalWeapon, true);
            PlayWeaponRevealSfx();
            yield return new WaitForSeconds(Mathf.Max(2.25f, _weaponRevealDuration));
        }

        private void BeginWeaponRollSfx()
        {
            ResolveWeaponRollClip();
            if (_weaponRollSfx == null) return;
            JcjSoundPlayback.PlayVfxLoop(_weaponRollSfx, _weaponRollSfxVolume, 1f, sceneTrim: 1f);
        }

        private void EndWeaponRollSfx() => JcjSoundPlayback.StopVfxLoop();

        private void ResolveWeaponRollClip()
        {
            if (_weaponRollSfx != null) return;
#if UNITY_EDITOR
            _weaponRollSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/#TeamFolder/JCJ/roll.wav");
#endif
        }

        private void PlayWeaponRevealSfx()
        {
            ResolveWeaponRevealClip();
            if (_weaponRevealSfx == null) return;
            JcjSoundPlayback.PlayVfx(_weaponRevealSfx, _weaponRevealSfxVolume, 1f, sceneTrim: 1f);
        }

        private void ResolveWeaponRevealClip()
        {
            if (_weaponRevealSfx != null) return;
#if UNITY_EDITOR
            _weaponRevealSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/#TeamFolder/JCJ/wow.mp3");
#endif
        }

        private void PlayCountdownTickSfx()
        {
            _cachedCountdownTickClip ??= SfxSynth.MakeCountdownTick();
            JcjSoundPlayback.PlayVfx(_cachedCountdownTickClip, _countdownSfxVolume, 1f, sceneTrim: 1f);
        }

        private void PlayCountdownGoSfx()
        {
            _cachedCountdownGoClip ??= SfxSynth.MakeGoBeep();
            JcjSoundPlayback.PlayVfx(_cachedCountdownGoClip, _countdownSfxVolume, 1f, sceneTrim: 1f);
        }

        // 비로컬 슬롯 무기 장착. 프로토타입은 PickWeapon으로 클라마다 랜덤이라 멀티에 부적합하다. 서버에서는 동일 WeaponId를 브로드캐스트한 뒤 여기서 EquipWeapon만 호출하도록 바꾸면 된다.
        /// <summary>로컬이 아닌 슬롯에 <see cref="PickWeapon"/>으로 무기를 맞춘다. <see cref="BeginMatchRoutine"/> 초반에서 호출.</summary>
        private void EquipRemoteWeapons()
        {
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                if (slot == null || slot.IsLocal || slot.WeaponManager == null) continue;
                if (slot.SelectedWeapon == null) slot.SelectedWeapon = PickWeapon(slot.Rank);
                if (slot.SelectedWeapon != null) slot.WeaponManager.EquipWeapon(slot.SelectedWeapon);
            }
        }

        // 입력·무기 입력·스폰 보호를 켠다. 서버 권한에서는 서버가 "게임플레이 시작" 신호를 줄 때만 이 경로를 타게 하면 동기화가 맞는다.
        /// <summary>매치 본편 입력·무기·스폰 보호 활성화. 인트로 종료 시와 <see cref="ApplyAuthoritativeRespawn"/> 등에서 사용.</summary>
        private void EnableGameplay()
        {
            if (_matchEnded) return;
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                if (slot == null) continue;
                if (slot.Controller != null)
                {
                    ApplyBattleSceneJumpPolicy(slot.Controller);
                    slot.Controller.SetGameplayInputEnabled(slot.IsLocal);
                }

                if (slot.WeaponManager != null) slot.WeaponManager.SetInputEnabled(slot.IsLocal);
                if (slot.Health != null) slot.Health.ActivateSpawnProtection(_spawnProtectionDuration);
                var boundary = slot.Instance != null ? slot.Instance.GetComponent<BattleArenaBoundary>() : null;
                if (boundary != null) boundary.SetSafePosition(slot.Instance.transform.position);
            }
        }

        private PlayerSlot GetLocalSlot()
        {
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                if (slot?.Instance == null) continue;
                var identity = RuntimePlayerIdentity.Find(slot.Instance.transform);
                if (identity != null && identity.IsLocalOwned) return slot;
                if (slot.IsLocal) return slot;
            }

            return null;
        }

        // 팀별 목표 점수 계산 지점이다.
        // 현재는 로컬 랭크 데이터로 계산하지만 서버 게임에서는 매치 시작 시점에 확정된 값으로 맞추는 편이 안전하다.
        private void CalculateTeamTargets()
        {
            _teamTargetScores[0] = 0;
            _teamTargetScores[1] = 0;
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                if (slot == null) continue;
                _teamTargetScores[slot.TeamIndex] += GetRankScoreValue(slot.Rank);
            }
        }

        private static int GetRankScoreValue(int rank)
        {
            return rank switch
            {
                1 => 5,
                2 => 4,
                3 => 3,
                _ => 2
            };
        }

        private void RefreshLeaderboard()
        {
            if (_leaderboardUI == null) return;
            _leaderboardUI.UpdateBoard(
                GetTeamName(0),
                GetTeamColor(0),
                _teamCurrentScores[0],
                _teamTargetScores[0],
                BuildTeamPlayerSummary(0),
                GetTeamName(1),
                GetTeamColor(1),
                _teamCurrentScores[1],
                _teamTargetScores[1],
                BuildTeamPlayerSummary(1));
        }

        private string BuildTeamPlayerSummary(int teamIndex)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                if (slot == null || slot.TeamIndex != teamIndex) continue;
                if (builder.Length > 0) builder.Append('\n');
                var identity = slot.Instance != null ? RuntimePlayerIdentity.Find(slot.Instance.transform) : null;
                builder.Append(identity != null ? identity.DisplayName : (slot.Instance != null ? slot.Instance.name : $"Player_{i + 1}"));
                builder.Append("  K:");
                builder.Append(slot.Kills);
                builder.Append("  D:");
                builder.Append(slot.Deaths);
            }

            return builder.ToString();
        }

        private void HandlePlayerDeath(BattleHealth victimHealth, BattleDamageInfo damageInfo)
        {
            if (_matchEnded) return; // 매치 종료 후에는 무시한다.
            if (!JcjRuntimeAuthority.UseLocalSimulation) // 서버 권한에서는 로컬 점수 처리를 하지 않는다.
            {
                if (!string.IsNullOrWhiteSpace(damageInfo.TargetId)) RespawnRequested?.Invoke(damageInfo.TargetId); // 패킷에 TargetId가 있으면 그대로 알린다.
                else
                {
                    var victimIdentity = RuntimePlayerIdentity.Find(victimHealth != null ? victimHealth.transform : null); // 없으면 컴포넌트에서 복구한다.
                    if (victimIdentity != null) RespawnRequested?.Invoke(victimIdentity.PlayerId); // 피해자 Id로 이벤트를 올린다.
                }
                return; // 로컬 킬 카운트와 코루틴 리스폰은 실행하지 않는다.
            }

            var victimSlot = FindSlot(victimHealth != null ? victimHealth.gameObject : null); // 이후는 순수 로컬 시뮬 경로이다.
            if (victimSlot == null) return; // 슬롯을 못 찾으면 아무 것도 안 한다.
            victimSlot.Deaths++; // 피해자 데스 카운트를 올린다.

            var attackerSlot = FindSlot(damageInfo.Attacker); // 공격자 오브젝트에서 슬롯을 찾는다.
            if (attackerSlot != null && attackerSlot != victimSlot && attackerSlot.TeamIndex != victimSlot.TeamIndex) // 다른 팀일 때만 킬 인정이다.
            {
                attackerSlot.Kills++; // 공격자 킬 수를 올린다.
                _teamCurrentScores[attackerSlot.TeamIndex]++; // 같은 팀 점수를 올린다.
                CheckForWinner(attackerSlot.TeamIndex); // 목표 점수 도달 여부를 검사한다.
            }

            RefreshLeaderboard(); // 점수판 문자열을 갱신한다.
            StartCoroutine(RespawnPlayerRoutine(victimSlot)); // 로컬에서는 코루틴으로 리스폰한다.
        }

        // 리스폰 위치와 보호 상태를 복구하는 단계다.
        // 서버를 붙이면 위치, 회전, 무적 종료 시각을 서버 기준으로 받은 뒤 이곳에서 씬에 적용하면 된다.
        private IEnumerator RespawnPlayerRoutine(PlayerSlot slot)
        {
            if (slot == null || slot.Instance == null || _matchEnded) yield break;

            yield return new WaitForSeconds(Mathf.Max(0.5f, _respawnDelay));

            if (_matchEnded || slot.Instance == null) yield break;

            Vector3 respawnPosition = SelectRespawnPosition(slot);
            slot.Instance.transform.SetPositionAndRotation(respawnPosition, Quaternion.identity);
            slot.Instance.SetActive(true);

            if (slot.Health != null)
            {
                slot.Health.ResetForRespawn();
                slot.Health.ActivateSpawnProtection(_spawnProtectionDuration);
            }

            if (slot.Controller != null)
            {
                ApplyBattleSceneJumpPolicy(slot.Controller);
                slot.Controller.SetGameplayInputEnabled(slot.IsLocal);
            }

            if (slot.WeaponManager != null) slot.WeaponManager.SetInputEnabled(slot.IsLocal);

            var boundary = slot.Instance.GetComponent<BattleArenaBoundary>();
            if (boundary != null) boundary.SetSafePosition(respawnPosition);
        }

        private Vector3 SelectRespawnPosition(PlayerSlot slot)
        {
            if (_spawnRoot == null || _spawnRoot.childCount == 0)
                return slot != null && slot.Instance != null ? slot.Instance.transform.position : Vector3.zero;

            Vector3 fallback = _spawnRoot.GetChild(0).position + Vector3.up * 0.65f;
            var rankedCandidates = new List<(Vector3 position, float enemyDistance)>(_spawnRoot.childCount);

            for (int i = 0; i < _spawnRoot.childCount; i++)
            {
                Vector3 candidate = _spawnRoot.GetChild(i).position + Vector3.up * 0.65f;
                if (IsSpawnBlocked(candidate)) continue;

                float nearestEnemyDistance = float.MaxValue;
                for (int j = 0; j < _playerSlots.Count; j++)
                {
                    var other = _playerSlots[j];
                    if (other == null || other == slot || other.TeamIndex == slot.TeamIndex || other.Instance == null) continue;
                    if (!other.Instance.activeInHierarchy) continue;
                    float sqrDistance = (other.Instance.transform.position - candidate).sqrMagnitude;
                    if (sqrDistance < nearestEnemyDistance) nearestEnemyDistance = sqrDistance;
                }

                rankedCandidates.Add((candidate, nearestEnemyDistance));
            }

            if (rankedCandidates.Count == 0) return fallback;

            rankedCandidates.Sort((a, b) => b.enemyDistance.CompareTo(a.enemyDistance));
            int choiceCount = Mathf.Min(3, rankedCandidates.Count);
            return rankedCandidates[Random.Range(0, choiceCount)].position;
        }

        private static bool IsSpawnBlocked(Vector3 position)
        {
            const float checkRadius = 0.35f;
            return Physics.CheckCapsule(
                position + Vector3.up * 0.15f,
                position + Vector3.up * 1.15f,
                checkRadius,
                ~0,
                QueryTriggerInteraction.Ignore);
        }

        private void CheckForWinner(int scoringTeamIndex)
        {
            if (_matchEnded) return;
            if (_teamCurrentScores[scoringTeamIndex] < _teamTargetScores[scoringTeamIndex]) return;

            _matchEnded = true;
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                if (slot == null) continue;
                if (slot.Controller != null) slot.Controller.SetGameplayInputEnabled(false);
                if (slot.WeaponManager != null) slot.WeaponManager.SetInputEnabled(false);
            }

            if (_leaderboardUI != null)
                _leaderboardUI.ShowWinner($"{GetTeamName(scoringTeamIndex)} WIN", GetTeamColor(scoringTeamIndex), true);
        }

        private PlayerSlot FindSlot(GameObject source)
        {
            if (source == null) return null;
            var sourceRoot = source.transform.root;
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                if (slot?.Instance == null) continue;
                if (slot.Instance == source || slot.Instance.transform.root == sourceRoot) return slot;
            }

            return null;
        }

        private void ApplyTeamPresentation()
        {
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                if (slot == null || slot.Health == null) continue;
                slot.Health.SetBaseTint(GetTeamColor(slot.TeamIndex));
            }
        }

        private BattleWeaponDefinition[] GetWeaponCandidates(int rank)
        {
            if (_weaponCatalog == null) return System.Array.Empty<BattleWeaponDefinition>();
            return _weaponCatalog.GetWeapons(BattleWeaponCatalog.RankToGrade(rank));
        }

        private BattleWeaponDefinition PickWeapon(int rank)
        {
            var candidates = GetWeaponCandidates(rank);
            if (candidates.Length == 0) return null;
            return candidates[Random.Range(0, candidates.Length)];
        }

        private Color GetTeamColor(int teamIndex)
        {
            return teamIndex == 0 ? _teamOneColor : _teamTwoColor;
        }

        private string GetTeamName(int teamIndex)
        {
            return teamIndex == 0 ? _teamOneName : _teamTwoName;
        }

        private int ResolveTeamIndex(int playerIndex)
        {
            if (_playerTeamIndices != null && playerIndex < _playerTeamIndices.Length)
                return Mathf.Clamp(_playerTeamIndices[playerIndex], 0, 1);
            return playerIndex < 2 ? 0 : 1;
        }

        // 에디터 기본 팀 섞기. 서버 매치메이킹 결과가 있으면 ApplyAuthoritativeMatchSetup에 넘기는 배열로 대체하는 편이 맞다.
        private void RandomizeTeamAssignments()
        {
            int playerCount = Mathf.Max(4, _playerRanks != null ? _playerRanks.Length : 4);
            _playerTeamIndices = new int[playerCount];
            var shuffled = new List<int>(playerCount);
            for (int i = 0; i < playerCount; i++) shuffled.Add(i);

            for (int i = 0; i < shuffled.Count; i++)
            {
                int swapIndex = Random.Range(i, shuffled.Count);
                (shuffled[i], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[i]);
            }

            int teamZeroCount = Mathf.Max(1, playerCount / 2);
            for (int i = 0; i < shuffled.Count; i++)
                _playerTeamIndices[shuffled[i]] = i < teamZeroCount ? 0 : 1;
        }

        private void EnsureSpawnRoot()
        {
            if (_spawnRoot != null) return;
            var root = new GameObject("BattleSpawnRoot");
            root.transform.SetParent(transform, false);
            _spawnRoot = root.transform;
            CreateSpawnPoint("Spawn_1", new Vector3(-12.5f, 0f, 10.5f));
            CreateSpawnPoint("Spawn_2", new Vector3(-12.5f, 0f, -10.5f));
            CreateSpawnPoint("Spawn_3", new Vector3(12.5f, 0f, 10.5f));
            CreateSpawnPoint("Spawn_4", new Vector3(12.5f, 0f, -10.5f));
            CreateSpawnPoint("Spawn_5", new Vector3(-14f, 0f, 2.5f));
            CreateSpawnPoint("Spawn_6", new Vector3(14f, 0f, 2.5f));
            CreateSpawnPoint("Spawn_7", new Vector3(-13f, 0f, -6.5f));
            CreateSpawnPoint("Spawn_8", new Vector3(13f, 0f, -6.5f));
            CreateSpawnPoint("Spawn_9", new Vector3(-4.5f, 0f, -12f));
            CreateSpawnPoint("Spawn_10", new Vector3(4.5f, 0f, -12f));
            CreateSpawnPoint("Spawn_11", new Vector3(-10.5f, 0f, 6f));
            CreateSpawnPoint("Spawn_12", new Vector3(10.5f, 0f, 6f));
        }

        private void CreateSpawnPoint(string pointName, Vector3 localPosition)
        {
            var point = new GameObject(pointName);
            point.transform.SetParent(_spawnRoot, false);
            point.transform.localPosition = localPosition;
        }

        // 로컬 시뮬 전용: Instantiate로 슬롯을 채운다. 네트워크에서는 서버 스폰 결과를 슬롯에 바인딩하거나, 이 로직을 팩토리로 분리해 재사용하는 것이 일반적이다.
        // RuntimePlayerIdentity.Configure의 playerId 문자열은 RespawnRequested·ApplyAuthoritativeRespawn과 짝을 맞추기 좋다.
        private void SpawnPlayers()
        {
            int playerCount = Mathf.Max(4, _playerRanks.Length);
            for (int i = 0; i < playerCount; i++)
            {
                Vector3 spawnPosition = SelectInitialSpawnPosition(i);
                var instance = Instantiate(_playerPrefab, spawnPosition, Quaternion.identity);
                instance.name = $"BattlePlayer_{i + 1}";
                instance.transform.localScale = Vector3.one * 0.65f;
                _players.Add(instance);

                bool isLocal = i == Mathf.Clamp(_localPlayerIndex, 0, playerCount - 1);
                int rank = ResolveRank(i);
                int teamIndex = ResolveTeamIndex(i);
                RuntimePlayerIdentity.Ensure(instance)?.Configure($"battle.player.{i + 1}", instance.name, i, isLocal);

                var controller = instance.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.IsLocalControlled = isLocal;
                    controller.SetGameplayInputEnabled(false);
                    ApplyBattleSceneJumpPolicy(controller);
                    ApplyBattleSceneBodyYawPolicy(controller);
                }

                var health = instance.GetComponent<BattleHealth>();
                if (health == null) health = instance.AddComponent<BattleHealth>();
                if (isLocal) health.HideHealthBar = true;
                health.SetTeamIndex(teamIndex);
                health.Died += HandlePlayerDeath;

                var weaponManager = instance.GetComponent<BattleWeaponManager>();
                if (weaponManager == null) weaponManager = instance.AddComponent<BattleWeaponManager>();
                weaponManager.SetAutoEquipOnStart(false);
                weaponManager.Configure(_weaponCatalog, isLocal, rank);
                weaponManager.SetInputEnabled(false);
                var preselectedWeapon = PickWeapon(rank);
                // 서버 모드에서는 무기를 여기서 장착하지 말고 네트워크 스냅샷의 WeaponId로 장착하는 편이 권장된다. 로컬 시뮬에선 인트로 전 미리보기용.
                if (isLocal && preselectedWeapon != null) weaponManager.EquipWeapon(preselectedWeapon);

                AttachHeadCollider(instance);
                AttachArenaBoundary(instance, spawnPosition);

                var slot = new PlayerSlot
                {
                    Instance = instance,
                    Controller = controller,
                    Health = health,
                    WeaponManager = weaponManager,
                    Rank = rank,
                    TeamIndex = teamIndex,
                    IsLocal = isLocal,
                    SelectedWeapon = preselectedWeapon
                };
                _playerSlots.Add(slot);

                if (isLocal)
                {
                    if (_battleCamera != null) _battleCamera.SetTarget(instance.transform);
                    var hud = gameObject.GetComponent<BattleAmmoHUD>();
                    if (hud == null) hud = gameObject.AddComponent<BattleAmmoHUD>();
                    hud.Bind(weaponManager);
                }
            }
        }

        public void ApplyAuthoritativeMatchSetup(int[] playerRanks, int[] playerTeamIndices)
        {
            int slotCountBefore = _playerSlots.Count; // 연출을 처음 시작해야 하는지 판별하기 위해 이전 개수를 저장한다.
            if (playerRanks != null && playerRanks.Length > 0) _playerRanks = (int[])playerRanks.Clone(); // 외부 배열을 변경하지 않도록 복사한다.
            if (playerTeamIndices != null && playerTeamIndices.Length > 0) _playerTeamIndices = (int[])playerTeamIndices.Clone(); // 동일하게 복사한다.
            if (_playerSlots.Count == 0) SpawnPlayers(); // 서버 데이터 도착 전까지 슬롯이 비어 있으면 여기서 로컬 스폰을 수행한다.
            ApplySavedPlayerSettings(); // 권한 매치 셋업으로 늦게 스폰된 플레이어에도 설정을 적용한다.
            CalculateTeamTargets(); // 새 랭크 합으로 목표 점수를 다시 계산한다.
            RefreshLeaderboard(); // UI에 즉시 반영한다.
            if (slotCountBefore == 0 && _playerSlots.Count > 0) StartMatchPresentation(); // 최초로 슬롯이 생긴 경우에만 인트로를 연다.
        }

        public void ApplyAuthoritativeRespawn(string playerId, Vector3 respawnPosition)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return; // 빈 Id는 적용할 대상이 없으므로 무시한다.
            for (int i = 0; i < _playerSlots.Count; i++) // 모든 슬롯에서 매칭되는 플레이어를 찾는다.
            {
                var slot = _playerSlots[i]; // 현재 슬롯 참조이다.
                if (slot?.Instance == null) continue; // 오브젝트가 없으면 건너뛴다.
                var identity = RuntimePlayerIdentity.Find(slot.Instance.transform); // PlayerId가 들어 있는 컴포넌트를 찾는다.
                if (identity == null || !string.Equals(identity.PlayerId, playerId, System.StringComparison.OrdinalIgnoreCase)) continue; // Id 불일치면 다음이다.

                slot.Instance.transform.SetPositionAndRotation(respawnPosition, Quaternion.identity); // 서버 좌표를 즉시 반영한다.
                slot.Instance.SetActive(true); // 비활성 상태였다면 다시 켠다.

                if (slot.Health != null)
                {
                    slot.Health.ResetForRespawn(); // 체력과 상태를 리셋한다.
                    slot.Health.ActivateSpawnProtection(_spawnProtectionDuration); // 짧은 무적을 준다.
                }

                if (slot.Controller != null)
                {
                    ApplyBattleSceneJumpPolicy(slot.Controller); // 점프 허용 정책을 다시 적용한다.
                    slot.Controller.SetGameplayInputEnabled(slot.IsLocal); // 로컬 슬롯만 입력을 살린다.
                }

                if (slot.WeaponManager != null) slot.WeaponManager.SetInputEnabled(slot.IsLocal); // 무기 입력도 로컬만 허용한다.

                var boundary = slot.Instance.GetComponent<BattleArenaBoundary>(); // 낙사 복구 컴포넌트를 찾는다.
                if (boundary != null) boundary.SetSafePosition(respawnPosition); // 안전 기준 좌표를 서버 스폰과 맞춘다.
                break; // 한 명만 처리하고 루프를 끝낸다.
            }
        }

        private Vector3 SelectInitialSpawnPosition(int playerIndex)
        {
            if (_spawnRoot == null || _spawnRoot.childCount == 0) return Vector3.up * 0.65f;

            var open = new List<(Vector3 position, float score)>(_spawnRoot.childCount);
            for (int i = 0; i < _spawnRoot.childCount; i++)
            {
                Vector3 candidate = _spawnRoot.GetChild(i).position + Vector3.up * 0.65f;
                if (IsSpawnBlocked(candidate)) continue;

                float nearestAlly = float.MaxValue;
                for (int j = 0; j < _players.Count; j++)
                {
                    if (_players[j] == null) continue;
                    float sqr = (_players[j].transform.position - candidate).sqrMagnitude;
                    if (sqr < nearestAlly) nearestAlly = sqr;
                }
                open.Add((candidate, nearestAlly));
            }

            if (open.Count == 0)
                return _spawnRoot.GetChild(Random.Range(0, _spawnRoot.childCount)).position + Vector3.up * 0.65f;

            open.Sort((a, b) => b.score.CompareTo(a.score));
            int pick = Mathf.Min(4, open.Count);
            return open[Random.Range(0, pick)].position;
        }

        private void AttachArenaBoundary(GameObject playerObject, Vector3 safePosition)
        {
            if (playerObject == null || _arenaRoot == null) return;
            var boundary = playerObject.GetComponent<BattleArenaBoundary>();
            if (boundary == null) boundary = playerObject.AddComponent<BattleArenaBoundary>();
            boundary.Configure(_arenaRoot, safePosition, _arenaBoundaryPadding, _arenaFallThresholdY, _arenaRecoverHeight);
        }

        private int ResolveRank(int playerIndex)
        {
            int playerCount = Mathf.Max(4, _playerRanks != null ? _playerRanks.Length : 0);
            if (_playerRanks == null || _playerRanks.Length == 0) return GetFallbackRank(playerIndex, playerCount);
            if (playerIndex < _playerRanks.Length) return Mathf.Clamp(_playerRanks[playerIndex], 1, playerCount);
            return GetFallbackRank(playerIndex, playerCount);
        }

        private void ResolveScoreService()
        {
            if (_scoreService == null && MatchScoreRankManager.Instance != null)
                _scoreService = MatchScoreRankManager.Instance.Score as ScoreService;
            if (_scoreService == null) _scoreService = ScoreService.Instance;
            if (_scoreService == null) _scoreService = Object.FindFirstObjectByType<ScoreService>();
            if (_scoreService == null && _matchScoreRankPrefab != null)
            {
                var root = Instantiate(_matchScoreRankPrefab);
                root.name = _matchScoreRankPrefab.name;
                _scoreService = root.GetComponent<ScoreService>();
            }
            if (_scoreService == null && _scoreServicePrefab != null)
            {
                var scoreServiceObject = Instantiate(_scoreServicePrefab);
                scoreServiceObject.name = _scoreServicePrefab.name;
                _scoreService = scoreServiceObject.GetComponent<ScoreService>();
            }
        }

        // 외부 점수 서비스에서 랭크를 읽어 배틀 시작값으로 반영한다.
        // 서버가 랭크/시드/매치메이킹 결과를 내려주면 이 지점에서 내부 배열만 치환하면 된다.
        private void ApplyRanksFromScoreService()
        {
            var score = (IScoreService)_scoreService ?? MatchScoreRankManager.Instance?.Score;
            if (!_useScoreServiceRanks || score == null) return;

            int playerCount = Mathf.Max(4, _playerRanks != null ? _playerRanks.Length : 0);
            var resolvedRanks = new int[playerCount];
            bool hasResolvedRank = false;

            for (int i = 0; i < playerCount; i++)
            {
                int rank = score.GetRankForPlayerIndex(i);
                if (rank > 0)
                {
                    resolvedRanks[i] = Mathf.Clamp(rank, 1, playerCount);
                    hasResolvedRank = true;
                }
                else
                {
                    resolvedRanks[i] = GetConfiguredRankOrDefault(i, playerCount);
                }
            }

            if (hasResolvedRank) _playerRanks = resolvedRanks;
        }

        private int GetFallbackRank(int playerIndex, int playerCount)
        {
            int clampedCount = Mathf.Max(1, playerCount);
            return Mathf.Clamp(playerIndex + 1, 1, clampedCount);
        }

        private int GetConfiguredRankOrDefault(int playerIndex, int playerCount)
        {
            if (_playerRanks != null && playerIndex < _playerRanks.Length)
                return Mathf.Clamp(_playerRanks[playerIndex], 1, Mathf.Max(1, playerCount));
            return GetFallbackRank(playerIndex, playerCount);
        }

        private void AttachHeadCollider(GameObject playerObject)
        {
            if (playerObject == null) return;
            var existing = playerObject.transform.Find("HeadHitbox");
            if (existing != null) return;

            var headObj = new GameObject("HeadHitbox");
            headObj.transform.SetParent(playerObject.transform, false);
            headObj.transform.localPosition = new Vector3(0f, _headLocalY, 0f);
            var sphere = headObj.AddComponent<SphereCollider>();
            sphere.radius = _headRadius;
        }

        private void ApplyBattleSceneJumpPolicy(PlayerController controller)
        {
            if (controller == null) return;
            controller.SetJumpEnabled(!_battleDisableJumpInThisScene);
        }

        private void ApplyBattleSceneBodyYawPolicy(PlayerController controller)
        {
            if (controller == null) return;
            controller.SetBattlePrototypeBodyYawDrive(true);
        }

        /// <summary>
        /// PlayerPrefs에 저장된 DPI·키 바인딩을 배틀 플레이어 InputActionMap에 반영한다.
        /// 플레이어가 런타임 스폰되므로 Maze/Tile과 달리 스폰 직후 한 번 더 호출해야 한다.
        /// </summary>
        private void ApplySavedPlayerSettings()
        {
            var settingsService = SettingsService.EnsureInstance();
            var data = settingsService?.Data;
            if (data == null) return;

            var keyBinder = Object.FindFirstObjectByType<KeyRebindBinder>();

            var rig = MazeCameraRig.Instance;
            if (rig != null) rig.SetAllowPitch(!data.lockPitch);

            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var controller = _playerSlots[i].Controller;
                if (controller == null) continue;

                controller.SetMouseSensitivity(data.cameraSensitivity);
                var map = controller.GetInputMap();
                if (map == null) continue;
                if (keyBinder != null) keyBinder.Register(map);
                else KeyRebindBinder.ApplyToMap(map, data);
            }
        }
    }
}
