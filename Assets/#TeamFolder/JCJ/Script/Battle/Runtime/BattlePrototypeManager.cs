using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using _TeamFolder.JCJ.Script;
using _TeamFolder.JCJ.Battle.Session;

// BattlePrototypeScene 중심 오케스트레이터. 전투 프로토타입의 라운드, 스폰, 팀, 점수를 총괄한다.
//
// 서버 연동 관점 요약
// - 장점: JcjRuntimeAuthority가 ServerAuthoritative면 Start에서 로컬 스폰·연출을 건너뛰고 MatchSetupRequested만 발생시킨다.
//   이후 네트워크 레이어가 ApplyAuthoritativeMatchSetup / ApplyAuthoritativeRespawn / StartMatchPresentation으로 씬 상태를 맞추면 된다.
// - BattleMatchRegistry에 등록되므로 다른 스크립트는 BattleMatchRegistry.Match로 IBattleMatchGateway에 접근할 수 있다.
// - 보완 예정으로 두기 좋은 부분: SpawnPlayers의 Instantiate를 네트워크 스폰으로 치환, 무기는 WeaponId 문자열 동기화 후 EquipWeapon,
//   킬/데스는 HandlePlayerDeath의 서버 분기처럼 서버 확정 이벤트만 반영하는 단일 경로로 통일.

namespace _TeamFolder.JCJ.Battle
{
    public class BattlePrototypeManager : MonoBehaviour, IBattleMatchGateway, IBattlePopupPresentation
    {
        // 슬롯당 전장에 한 명. 서버 모델에서는 Instance가 네트워크 바디이고 IsLocal은 입력·카메라를 붙일 로컬 소유자만 true.
        private sealed class PlayerSlot
        {
            public GameObject Instance;
            public PlayerController Controller;
            public BattleHealth Health;
            public BattleWeaponManager WeaponManager;
            public int Rank;
            public int TeamIndex;
            public bool IsLocal;
            public BattleWeaponDefinition SelectedWeapon;
            public int Kills;
            public int Deaths;
        }

        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private BattleWeaponCatalog _weaponCatalog;
        [SerializeField] private BattleFirstPersonCamera _battleCamera;
        [SerializeField] private ScoreService _scoreService;
        [SerializeField] private GameObject _scoreServicePrefab;
        [SerializeField] private bool _useScoreServiceRanks = true;
        [SerializeField] private Transform _spawnRoot;
        [SerializeField] private int _localPlayerIndex;
        [SerializeField] private int[] _playerRanks = { 2, 3, 1, 4 };
        [SerializeField] private int[] _playerTeamIndices = { 0, 1, 0, 1 };
        [SerializeField] private Color _teamOneColor = new(0.22f, 0.72f, 1f, 1f);
        [SerializeField] private Color _teamTwoColor = new(1f, 0.36f, 0.36f, 1f);
        [SerializeField] private string _teamOneName = "BLUE TEAM";
        [SerializeField] private string _teamTwoName = "RED TEAM";
        [SerializeField] private float _weaponRollDuration = 1.2f;
        [SerializeField] private float _weaponRevealDuration = 1f;
        [SerializeField] private float _countdownStepDuration = 1f;
        [SerializeField] private int _countdownStart = 3;
        [SerializeField] private float _respawnDelay = 3f;
        [SerializeField] private float _spawnProtectionDuration = 2f;
        [SerializeField] private float _arenaBoundaryPadding = 1.4f;
        [SerializeField] private float _arenaFallThresholdY = -6f;
        [SerializeField] private float _arenaRecoverHeight = 1f;
        [SerializeField] private float _headLocalY = 0.72f;
        [SerializeField] private float _headRadius = 0.65f;
        [SerializeField] private float _popupScale = 0.005f;
        [SerializeField] private int _popupFontSize = 130;
        [SerializeField] private int _popupHeadshotFontSize = 180;
        [SerializeField] private bool _battleUseThirdPersonCamera;
        [SerializeField] private bool _battleDisableJumpInThisScene;

        private readonly List<GameObject> _players = new();
        private readonly List<PlayerSlot> _playerSlots = new();

        private static BattlePrototypeManager _instance;
        private Vector3 _originalGravity;
        private BattleIntroUI _introUI;
        private BattleLeaderboardUI _leaderboardUI;
        private Transform _arenaRoot;
        private int[] _teamTargetScores = new int[2];
        private int[] _teamCurrentScores = new int[2];
        private bool _matchEnded;
        private bool _matchPresentationStarted;

        // 네트워크 매니저가 씬 로드 직후 구독해, 서버에서 받은 랭크·팀·스폰 정보로 ApplyAuthoritativeMatchSetup을 호출하는 진입 신호로 쓰면 된다.
        public event System.Action MatchSetupRequested;

        // 서버 권한 데스 처리: 플레이어Id만 넘기고 서버가 좌표 계산 후 ApplyAuthoritativeRespawn을 보내기 전 알림용. 로컬 시뮬에선 사용하지 않는다.
        public event System.Action<string> RespawnRequested;

        float IBattlePopupPresentation.DamagePopupWorldScale => _popupScale;
        int IBattlePopupPresentation.DamagePopupFontSize => _popupFontSize;
        int IBattlePopupPresentation.DamagePopupHeadshotFontSize => _popupHeadshotFontSize;

        public static float PopupScale =>
            BattleMatchRegistry.Popups?.DamagePopupWorldScale ?? BattleMatchRegistry.DefaultDamagePopupWorldScale;
        public static int PopupFontSize =>
            BattleMatchRegistry.Popups?.DamagePopupFontSize ?? BattleMatchRegistry.DefaultDamagePopupFontSize;
        public static int PopupHeadshotFontSize =>
            BattleMatchRegistry.Popups?.DamagePopupHeadshotFontSize ?? BattleMatchRegistry.DefaultDamagePopupHeadshotFontSize;

        // 배틀 라운드 초기 진입점.
        // LocalSimulation: 이 매니저가 SpawnPlayers부터 인트로까지 전부 담당한다.
        // ServerAuthoritative: 여기서는 물리·UI 셸만 준비하고 MatchSetupRequested로 네트워크 레이어에 넘긴다. 실제 스폰은 ApplyAuthoritativeMatchSetup 쪽이 담당.
        private void Start()
        {
            _instance = this;
            BattleMatchRegistry.Register(this);
            _originalGravity = Physics.gravity;
            Physics.gravity = new Vector3(0f, -25f, 0f);

            if (_playerPrefab == null || _weaponCatalog == null) return;

            if (_battleCamera == null) _battleCamera = Object.FindFirstObjectByType<BattleFirstPersonCamera>();
            if (_battleCamera != null) _battleCamera.SetThirdPersonMode(_battleUseThirdPersonCamera);
            ResolveScoreService();
            ApplyRanksFromScoreService();
            RandomizeTeamAssignments();
            ResolveArenaRoot();
            EnsureSpawnRoot();
            EnsureIntroUI();
            EnsureLeaderboardUI();
            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                MatchSetupRequested?.Invoke();
                return;
            }
            SpawnPlayers();
            CalculateTeamTargets();
            RefreshLeaderboard();
            StartMatchPresentation();
        }

        // 서버에서 playerRanks·팀이 확정된 뒤, 슬롯이 비어 있지 않다면 인트로와 동일한 연출을 돌리고 싶을 때 호출한다.
        public void StartMatchPresentation()
        {
            if (_matchPresentationStarted) return;
            _matchPresentationStarted = true;
            StartCoroutine(BeginMatchRoutine());
        }

        private void OnDestroy()
        {
            Physics.gravity = _originalGravity;
            if (_instance == this)
            {
                BattleMatchRegistry.Unregister(this);
                _instance = null;
            }
        }

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

        private void EnsureIntroUI()
        {
            _introUI = GetComponent<BattleIntroUI>();
            if (_introUI == null) _introUI = gameObject.AddComponent<BattleIntroUI>();
        }

        private void EnsureLeaderboardUI()
        {
            _leaderboardUI = GetComponent<BattleLeaderboardUI>();
            if (_leaderboardUI == null) _leaderboardUI = gameObject.AddComponent<BattleLeaderboardUI>();
        }

        private void ResolveArenaRoot()
        {
            if (_arenaRoot != null) return;
            var arenaObject = GameObject.Find("DeathmatchArena");
            if (arenaObject != null) _arenaRoot = arenaObject.transform;
        }

        // 시작 연출, 팀 표시, 무기 지급, 입력 허용 순서를 묶는 코루틴.
        // 서버 연동 시 권장: 서버가 "매치 시작 시각" 또는 단계 enum을 브로드캐스트하고, 클라는 그에 맞춰 카운트다운·입력 해제만 수행.
        // 무기 최종값은 서버가 준 WeaponId로 EquipWeapon하는 경로로 바꾸면 PlayLocalWeaponDraw의 Random 최종무기와 충돌하지 않는다.
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
                    yield return new WaitForSeconds(_countdownStepDuration);
                }

                _introUI.ShowCountdown(GetTeamName(localSlot.TeamIndex), GetTeamColor(localSlot.TeamIndex), 0);
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
        private IEnumerator PlayLocalWeaponDraw(PlayerSlot slot)
        {
            var candidates = GetWeaponCandidates(slot.Rank);
            if (candidates.Length == 0) yield break;

            var finalWeapon = candidates[Random.Range(0, candidates.Length)];
            float endTime = Time.time + Mathf.Max(0.1f, _weaponRollDuration);
            int lastIndex = -1;

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

            slot.SelectedWeapon = finalWeapon;
            slot.WeaponManager.EquipWeapon(finalWeapon);
            _introUI.ShowWeaponRoll(GetTeamName(slot.TeamIndex), GetTeamColor(slot.TeamIndex), slot.Rank, finalWeapon, true);
            yield return new WaitForSeconds(Mathf.Max(2.25f, _weaponRevealDuration));
        }

        // 비로컬 슬롯 무기 장착. 프로토타입은 PickWeapon으로 클라마다 랜덤이라 멀티에 부적합하다. 서버에서는 동일 WeaponId를 브로드캐스트한 뒤 여기서 EquipWeapon만 호출하도록 바꾸면 된다.
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

        // 킬, 데스, 팀 점수 갱신이 한 번에 모이는 지점이다.
        // 서버 연동 시에는 이 메서드가 로컬 판정 결과를 소비하는 곳이 아니라 서버 확정 킬 이벤트를 반영하는 쪽이 자연스럽다.
        private void HandlePlayerDeath(BattleHealth victimHealth, BattleDamageInfo damageInfo)
        {
            if (_matchEnded) return;
            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                if (!string.IsNullOrWhiteSpace(damageInfo.TargetId)) RespawnRequested?.Invoke(damageInfo.TargetId);
                else
                {
                    var victimIdentity = RuntimePlayerIdentity.Find(victimHealth != null ? victimHealth.transform : null);
                    if (victimIdentity != null) RespawnRequested?.Invoke(victimIdentity.PlayerId);
                }
                return;
            }

            var victimSlot = FindSlot(victimHealth != null ? victimHealth.gameObject : null);
            if (victimSlot == null) return;
            victimSlot.Deaths++;

            var attackerSlot = FindSlot(damageInfo.Attacker);
            if (attackerSlot != null && attackerSlot != victimSlot && attackerSlot.TeamIndex != victimSlot.TeamIndex)
            {
                attackerSlot.Kills++;
                _teamCurrentScores[attackerSlot.TeamIndex]++;
                CheckForWinner(attackerSlot.TeamIndex);
            }

            RefreshLeaderboard();
            StartCoroutine(RespawnPlayerRoutine(victimSlot));
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

        // IBattleMatchGateway. 서버(또는 호스트)가 확정한 랭크·팀 배열을 넣고 호출한다. 슬롯이 비어 있으면 SpawnPlayers까지 수행한다.
        public void ApplyAuthoritativeMatchSetup(int[] playerRanks, int[] playerTeamIndices)
        {
            int slotCountBefore = _playerSlots.Count;
            if (playerRanks != null && playerRanks.Length > 0) _playerRanks = (int[])playerRanks.Clone();
            if (playerTeamIndices != null && playerTeamIndices.Length > 0) _playerTeamIndices = (int[])playerTeamIndices.Clone();
            if (_playerSlots.Count == 0) SpawnPlayers();
            CalculateTeamTargets();
            RefreshLeaderboard();
            if (slotCountBefore == 0 && _playerSlots.Count > 0) StartMatchPresentation();
        }

        // IBattleMatchGateway. 서버가 계산한 리스폰 좌표를 즉시 적용한다. 로컬 코루틴 RespawnPlayerRoutine과 역할이 겹치므로 서버 모드에서는 한쪽만 쓰는 것이 좋다.
        public void ApplyAuthoritativeRespawn(string playerId, Vector3 respawnPosition)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return;
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                if (slot?.Instance == null) continue;
                var identity = RuntimePlayerIdentity.Find(slot.Instance.transform);
                if (identity == null || !string.Equals(identity.PlayerId, playerId, System.StringComparison.OrdinalIgnoreCase)) continue;

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
                break;
            }
        }

        private Vector3 SelectInitialSpawnPosition(int playerIndex)
        {
            if (_spawnRoot == null || _spawnRoot.childCount == 0) return Vector3.up * 0.65f;
            int cornerIndex = Mathf.Clamp(playerIndex, 0, Mathf.Min(3, _spawnRoot.childCount - 1));
            Vector3 primaryCorner = _spawnRoot.GetChild(cornerIndex).position + Vector3.up * 0.65f;
            if (!IsSpawnBlocked(primaryCorner)) return primaryCorner;

            for (int i = 0; i < _spawnRoot.childCount; i++)
            {
                Vector3 candidate = _spawnRoot.GetChild(i).position + Vector3.up * 0.65f;
                if (!IsSpawnBlocked(candidate)) return candidate;
            }

            return primaryCorner;
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
            if (_scoreService == null) _scoreService = ScoreService.Instance;
            if (_scoreService == null) _scoreService = Object.FindFirstObjectByType<ScoreService>();
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
            if (!_useScoreServiceRanks || _scoreService == null) return;

            int playerCount = Mathf.Max(4, _playerRanks != null ? _playerRanks.Length : 0);
            var resolvedRanks = new int[playerCount];
            bool hasResolvedRank = false;

            for (int i = 0; i < playerCount; i++)
            {
                int rank = _scoreService.GetRankForPlayerIndex(i);
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
    }
}
