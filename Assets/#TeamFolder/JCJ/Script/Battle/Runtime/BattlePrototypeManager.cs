using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using _TeamFolder.JCJ.Script;

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

        public static float PopupScale => _instance != null ? _instance._popupScale : 0.005f;
        public static int PopupFontSize => _instance != null ? _instance._popupFontSize : 130;
        public static int PopupHeadshotFontSize => _instance != null ? _instance._popupHeadshotFontSize : 180;

        private void Start()
        {
            _instance = this;
            _originalGravity = Physics.gravity;
            Physics.gravity = new Vector3(0f, -25f, 0f);

            if (_playerPrefab == null || _weaponCatalog == null) return;

            if (_battleCamera == null) _battleCamera = Object.FindFirstObjectByType<BattleFirstPersonCamera>();
            ResolveScoreService();
            ApplyRanksFromScoreService();
            EnsureSpawnRoot();
            ResolveArenaRoot();
            EnsureIntroUI();
            EnsureLeaderboardUI();
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
                if (slot.Controller != null) slot.Controller.SetGameplayInputEnabled(slot.IsLocal);
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
                if (slot != null && slot.IsLocal) return slot;
            }

            return null;
        }

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
                BuildTeamTargetReason(0),
                BuildTeamPlayerSummary(0),
                GetTeamName(1),
                GetTeamColor(1),
                _teamCurrentScores[1],
                _teamTargetScores[1],
                BuildTeamTargetReason(1),
                BuildTeamPlayerSummary(1));
        }

        private string BuildTeamTargetReason(int teamIndex)
        {
            var builder = new StringBuilder("GOAL = ");
            bool appended = false;
            int total = 0;
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                if (slot == null || slot.TeamIndex != teamIndex) continue;
                if (appended) builder.Append(" + ");
                int score = GetRankScoreValue(slot.Rank);
                builder.Append(GetRankLabel(slot.Rank));
                builder.Append("(+");
                builder.Append(score);
                builder.Append(')');
                total += score;
                appended = true;
            }

            builder.Append(" = ");
            builder.Append(total);
            return builder.ToString();
        }

        private static string GetRankLabel(int rank)
        {
            return rank switch
            {
                1 => "1st",
                2 => "2nd",
                3 => "3rd",
                _ => $"{rank}th"
            };
        }

        private string BuildTeamPlayerSummary(int teamIndex)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                if (slot == null || slot.TeamIndex != teamIndex) continue;
                if (builder.Length > 0) builder.Append('\n');
                builder.Append(slot.Instance != null ? slot.Instance.name : $"Player_{i + 1}");
                builder.Append("  K:");
                builder.Append(slot.Kills);
                builder.Append("  D:");
                builder.Append(slot.Deaths);
                builder.Append("  R:");
                builder.Append(slot.Rank);
                builder.Append("  +");
                builder.Append(GetRankScoreValue(slot.Rank));
            }

            return builder.ToString();
        }

        private void HandlePlayerDeath(BattleHealth victimHealth, BattleDamageInfo damageInfo)
        {
            if (_matchEnded) return;

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

            if (slot.Controller != null) slot.Controller.SetGameplayInputEnabled(slot.IsLocal);
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
            const float checkRadius = 0.45f;
            return Physics.CheckSphere(position + Vector3.up * 0.4f, checkRadius, ~0, QueryTriggerInteraction.Ignore);
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
            return playerIndex < 2 ? 0 : 1;
        }

        private void EnsureSpawnRoot()
        {
            if (_spawnRoot != null) return;
            var root = new GameObject("BattleSpawnRoot");
            root.transform.SetParent(transform, false);
            _spawnRoot = root.transform;
            CreateSpawnPoint("Spawn_1", new Vector3(-18f, 0f, -12f));
            CreateSpawnPoint("Spawn_2", new Vector3(-18f, 0f, 12f));
            CreateSpawnPoint("Spawn_3", new Vector3(18f, 0f, -12f));
            CreateSpawnPoint("Spawn_4", new Vector3(18f, 0f, 12f));
            CreateSpawnPoint("Spawn_5", new Vector3(-8f, 0f, -20f));
            CreateSpawnPoint("Spawn_6", new Vector3(8f, 0f, -20f));
            CreateSpawnPoint("Spawn_7", new Vector3(-8f, 0f, 20f));
            CreateSpawnPoint("Spawn_8", new Vector3(8f, 0f, 20f));
            CreateSpawnPoint("Spawn_9", new Vector3(-23f, 0f, 0f));
            CreateSpawnPoint("Spawn_10", new Vector3(23f, 0f, 0f));
            CreateSpawnPoint("Spawn_11", new Vector3(0f, 0f, -23f));
            CreateSpawnPoint("Spawn_12", new Vector3(0f, 0f, 23f));
        }

        private void CreateSpawnPoint(string pointName, Vector3 localPosition)
        {
            var point = new GameObject(pointName);
            point.transform.SetParent(_spawnRoot, false);
            point.transform.localPosition = localPosition;
        }

        private void SpawnPlayers()
        {
            int playerCount = Mathf.Max(4, _playerRanks.Length);
            for (int i = 0; i < playerCount; i++)
            {
                var spawn = i < _spawnRoot.childCount ? _spawnRoot.GetChild(i) : _spawnRoot;
                var instance = Instantiate(_playerPrefab, spawn.position + Vector3.up * 0.65f, Quaternion.identity);
                instance.name = $"BattlePlayer_{i + 1}";
                instance.transform.localScale = Vector3.one * 0.65f;
                _players.Add(instance);

                bool isLocal = i == Mathf.Clamp(_localPlayerIndex, 0, playerCount - 1);
                int rank = ResolveRank(i);
                int teamIndex = ResolveTeamIndex(i);

                var controller = instance.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.IsLocalControlled = isLocal;
                    controller.SetGameplayInputEnabled(false);
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

                AttachHeadCollider(instance);
                AttachArenaBoundary(instance, spawn.position + Vector3.up * 0.65f);

                var slot = new PlayerSlot
                {
                    Instance = instance,
                    Controller = controller,
                    Health = health,
                    WeaponManager = weaponManager,
                    Rank = rank,
                    TeamIndex = teamIndex,
                    IsLocal = isLocal,
                    SelectedWeapon = isLocal ? null : PickWeapon(rank)
                };
                _playerSlots.Add(slot);

                if (isLocal)
                {
                    HideLocalVisuals(instance);
                    if (_battleCamera != null) _battleCamera.SetTarget(instance.transform);
                    var hud = gameObject.GetComponent<BattleAmmoHUD>();
                    if (hud == null) hud = gameObject.AddComponent<BattleAmmoHUD>();
                    hud.Bind(weaponManager);
                }
            }
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
            headObj.tag = "BattleHead";
            headObj.transform.SetParent(playerObject.transform, false);
            headObj.transform.localPosition = new Vector3(0f, _headLocalY, 0f);
            var sphere = headObj.AddComponent<SphereCollider>();
            sphere.radius = _headRadius;
        }

        private static void HideLocalVisuals(GameObject playerObject)
        {
            if (playerObject == null) return;
            foreach (var renderer in playerObject.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }
    }
}
