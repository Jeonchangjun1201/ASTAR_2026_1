using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using _TeamFolder.JCJ.Script;

// 전투 프로토타입 라운드 흐름과 스폰, 팀, 점수를 총괄하는 매니저.

namespace _TeamFolder.JCJ.Battle
{
    public class BattlePrototypeManager : MonoBehaviour
    {
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
        public event System.Action MatchSetupRequested;
        public event System.Action<string> RespawnRequested;

        public static float PopupScale => _instance != null ? _instance._popupScale : 0.005f;
        public static int PopupFontSize => _instance != null ? _instance._popupFontSize : 130;
        public static int PopupHeadshotFontSize => _instance != null ? _instance._popupHeadshotFontSize : 180;

        // 배틀 라운드 초기 진입점이다.
        // 지금은 씬 로컬 기준으로 준비를 끝내고 바로 플레이어를 생성한다.
        private void Start()
        {
            _instance = this;
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
            StartCoroutine(BeginMatchRoutine());
        }

        private void OnDestroy()
        {
            Physics.gravity = _originalGravity;
            if (_instance == this) _instance = null;
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

        // 시작 연출, 팀 표시, 무기 지급, 입력 허용 순서를 묶는 코루틴이다.
        // 서버를 붙이면 이 순서는 서버 상태를 받아 UI와 입력 잠금을 맞추는 흐름으로 바뀌기 쉽다.
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

        // 실제 플레이어 인스턴스를 만들고 슬롯 정보를 연결하는 생성 단계다.
        // 나중에 서버 스폰을 붙이면 이 함수는 "직접 생성"보다 "서버가 준 오브젝트/소유권을 슬롯에 연결"하는 역할로 바뀔 가능성이 크다.
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
            if (playerRanks != null && playerRanks.Length > 0) _playerRanks = (int[])playerRanks.Clone();
            if (playerTeamIndices != null && playerTeamIndices.Length > 0) _playerTeamIndices = (int[])playerTeamIndices.Clone();
            if (_playerSlots.Count == 0) SpawnPlayers();
            CalculateTeamTargets();
            RefreshLeaderboard();
        }

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

        public static void ApplyLocalThirdPersonBodyLayersToPlayer(GameObject playerRoot)
        {
            if (playerRoot == null) return;
            int defaultLayer = 0;
            var renderers = playerRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (r is LineRenderer) continue;
                r.gameObject.layer = defaultLayer;
            }
        }

        public static void ApplyLocalFirstPersonBodyLayersToPlayer(GameObject playerRoot)
        {
            if (playerRoot == null) return;
            int lb = LayerMask.NameToLayer("BattleLocalBody");
            if (lb < 0) return;
            Transform camT = null;
            var fpc = BattleFirstPersonCamera.Instance;
            if (fpc != null) camT = fpc.transform;
            Transform weaponMount = playerRoot.transform.Find("WeaponMount");
            int defaultLayer = 0;
            var renderers = playerRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (r is LineRenderer) continue;
                if (camT != null && (r.transform == camT || r.transform.IsChildOf(camT))) continue;
                if (weaponMount != null && (r.transform == weaponMount || r.transform.IsChildOf(weaponMount)))
                {
                    r.gameObject.layer = defaultLayer;
                    continue;
                }

                r.gameObject.layer = lb;
            }
        }
    }
}
