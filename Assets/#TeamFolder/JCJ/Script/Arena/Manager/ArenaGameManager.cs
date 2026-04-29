using System;
using System.Collections.Generic;
using UnityEngine;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Script.Arena
{
    public class ArenaGameManager : MonoBehaviour
    {
        [SerializeField] private bool _autoStartOnPlay;
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Vector3 _spawnCenter = Vector3.zero;
        [SerializeField] private float _spawnRadius = 9f;
        [SerializeField] private int _standaloneStartingScore = ArenaDesignValues.StandaloneSeedScore;
        [SerializeField] private List<ArenaPlayerSpawnEntry> _playerEntries = new();

        private readonly List<ArenaPlayerSessionState> _sessions = new();
        private readonly List<ArenaPlayerController> _controllers = new();
        private readonly List<string> _eliminationOrder = new();
        private readonly Dictionary<string, float> _botPrepDecisionAt = new();
        private ArenaServerBridge _serverBridge;
        private ArenaSceneRuntimeSetup _runtimeSetup;
        private string _gameSessionId;
        private string _phaseId;
        private float _phaseRemaining;

        public static ArenaGameManager Instance { get; private set; }

        public event Action<ArenaPhase> OnPhaseChanged;
        public event Action<float> OnPhaseTimerChanged;
        public event Action<ArenaModeType> OnModeChanged;
        public event Action OnSessionsChanged;
        public event Action<string> OnTooltipRequested;

        public ArenaPhase CurrentPhase { get; private set; } = ArenaPhase.Inactive;
        public ArenaModeType CurrentMode { get; private set; } = ArenaModeType.FreeForAll;
        public string GameSessionId => _gameSessionId;
        public string PhaseId => _phaseId;
        public ArenaServerBridge ServerBridge => _serverBridge;
        public IReadOnlyList<ArenaPlayerSessionState> Sessions => _sessions;
        public IReadOnlyList<ArenaPlayerController> Controllers => _controllers;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _serverBridge = GetComponent<ArenaServerBridge>();
            if (_serverBridge == null)
            {
                _serverBridge = gameObject.AddComponent<ArenaServerBridge>();
            }

            _runtimeSetup = GetComponent<ArenaSceneRuntimeSetup>();
            if (_runtimeSetup == null)
            {
                _runtimeSetup = gameObject.AddComponent<ArenaSceneRuntimeSetup>();
            }

            _runtimeSetup.ApplyRuntimeSetup();
            _spawnRadius = Mathf.Max(_spawnRadius, 9f);

            if (FindFirstObjectByType<ArenaPrepHud>() == null)
            {
                new GameObject("ArenaPrepHud").AddComponent<ArenaPrepHud>();
            }

            EnsurePlayers();
            BuildSessions();
        }

        private void Start()
        {
            if (_autoStartOnPlay)
            {
                BeginMinigame();
            }
        }

        private void Update()
        {
            if (CurrentPhase == ArenaPhase.Inactive)
            {
                return;
            }

            if (CurrentPhase == ArenaPhase.Preparation)
            {
                UpdatePreparationBots();
            }

            _phaseRemaining -= Time.deltaTime;
            OnPhaseTimerChanged?.Invoke(Mathf.Max(0f, _phaseRemaining));
            if (_phaseRemaining > 0f)
            {
                return;
            }

            if (CurrentPhase == ArenaPhase.Preparation)
            {
                StartCombatPhase();
                return;
            }

            if (CurrentPhase == ArenaPhase.Playing)
            {
                FinishRound();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [ContextMenu("Begin Arena Minigame")]
        public void BeginMinigame()
        {
            _phaseId = Guid.NewGuid().ToString("N");
            _eliminationOrder.Clear();
            _botPrepDecisionAt.Clear();
            SelectMode();
            AssignTeams();
            SpawnPlayersForCurrentMode();
            SyncCameraTarget();
            SetPhase(ArenaPhase.Preparation, ArenaDesignValues.PreparationDurationSeconds);

            for (int i = 0; i < _sessions.Count; i++)
            {
                _sessions[i].IsReady = false;
                _sessions[i].IsAlive = true;
                _sessions[i].Placement = 0;
            }

            SyncControllersFromSessions();
            OnSessionsChanged?.Invoke();
        }

        public void ResetEntireGameSession()
        {
            _gameSessionId = Guid.NewGuid().ToString("N");
            for (int i = 0; i < _sessions.Count; i++)
            {
                _sessions[i].StoredScore = _playerEntries[i].InitialScore > 0
                    ? Mathf.Max(0, _playerEntries[i].InitialScore)
                    : Mathf.Max(0, _standaloneStartingScore);
                _sessions[i].PurchasedNodes.Clear();
                _sessions[i].ResolvedStats = ArenaSkillCatalog.ResolveStats(_sessions[i].PurchasedNodes);
                _sessions[i].IsReady = false;
                _sessions[i].IsAlive = true;
                _sessions[i].Placement = 0;
            }

            OnSessionsChanged?.Invoke();
        }

        public bool TryPurchaseNode(string playerId, ArenaNodeId nodeId, out string message)
        {
            var session = FindSession(playerId);
            if (session == null)
            {
                message = "플레이어 없음";
                return false;
            }

            if (CurrentPhase != ArenaPhase.Preparation)
            {
                message = "준비 페이즈 아님";
                return false;
            }

            if (!ArenaSkillCatalog.CanPurchase(session, nodeId, out message))
            {
                return false;
            }

            var definition = ArenaSkillCatalog.Get(nodeId);
            session.StoredScore -= definition.Cost;
            session.PurchasedNodes.Add(nodeId);
            session.ResolvedStats = ArenaSkillCatalog.ResolveStats(session.PurchasedNodes);
            SyncControllersFromSessions();
            OnSessionsChanged?.Invoke();
            message = "구매 완료";
            return true;
        }

        public void SetPlayerReady(string playerId, bool isReady)
        {
            var session = FindSession(playerId);
            if (session == null)
            {
                return;
            }

            session.IsReady = isReady;
            var controller = FindController(playerId);
            if (controller != null)
            {
                controller.SetPreparationReady(isReady);
            }

            OnSessionsChanged?.Invoke();
            if (CurrentPhase == ArenaPhase.Preparation && AreAllPlayersReady())
            {
                StartCombatPhase();
            }
        }

        public void NotifyCombatAction(ArenaPlayerController source, ArenaCombatActionType actionType, string targetId, int chargeMs, Vector3 direction)
        {
            if (_serverBridge == null || source == null)
            {
                return;
            }

            _ = _serverBridge.BuildCombatActionJson(_gameSessionId, source.PlayerId, actionType, targetId, chargeMs, direction);
        }

        public void RequestTooltip(string tooltip)
        {
            OnTooltipRequested?.Invoke(tooltip);
        }

        public ArenaPlayerSessionState GetLocalSession()
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (_playerEntries[i].IsLocalControlled)
                {
                    return _sessions[i];
                }
            }

            return _sessions.Count > 0 ? _sessions[0] : null;
        }

        public ArenaPlayerController GetPreferredFollowTarget()
        {
            for (int i = 0; i < _controllers.Count; i++)
            {
                if (_controllers[i] != null && _controllers[i].IsLocalControlled && _controllers[i].IsAlive)
                {
                    return _controllers[i];
                }
            }

            for (int i = 0; i < _controllers.Count; i++)
            {
                if (_controllers[i] != null && _controllers[i].IsAlive)
                {
                    return _controllers[i];
                }
            }

            for (int i = 0; i < _controllers.Count; i++)
            {
                if (_controllers[i] != null)
                {
                    return _controllers[i];
                }
            }

            var discoveredControllers = FindObjectsByType<ArenaPlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < discoveredControllers.Length; i++)
            {
                if (discoveredControllers[i] != null && discoveredControllers[i].IsLocalControlled && discoveredControllers[i].IsAlive)
                {
                    return discoveredControllers[i];
                }
            }

            for (int i = 0; i < discoveredControllers.Length; i++)
            {
                if (discoveredControllers[i] != null && discoveredControllers[i].IsAlive)
                {
                    return discoveredControllers[i];
                }
            }

            return discoveredControllers.Length > 0 ? discoveredControllers[0] : null;
        }

        public Vector3 GetArenaCenter()
        {
            return _spawnCenter;
        }

        public static int GetPlacementScoreStatic(int placement)
        {
            return placement switch
            {
                1 => ArenaDesignValues.FirstPlaceScore,
                2 => ArenaDesignValues.SecondPlaceScore,
                3 => ArenaDesignValues.ThirdPlaceScore,
                _ => ArenaDesignValues.FourthPlaceScore
            };
        }

        private void StartCombatPhase()
        {
            SetPhase(ArenaPhase.Playing, ArenaDesignValues.RoundDurationSeconds);
            for (int i = 0; i < _controllers.Count; i++)
            {
                _controllers[i].HealToFull();
                _controllers[i].SetPreparationReady(false);
                _sessions[i].IsAlive = true;
                _sessions[i].Placement = 0;
            }

            SyncCameraTarget();
            OnSessionsChanged?.Invoke();
        }

        private void FinishRound()
        {
            if (CurrentPhase != ArenaPhase.Playing)
            {
                return;
            }

            AssignPlacements();
            ApplyPlacementScores();
            SetPhase(ArenaPhase.Finished, 0f);
            OnSessionsChanged?.Invoke();
        }

        private void SetPhase(ArenaPhase nextPhase, float duration)
        {
            CurrentPhase = nextPhase;
            _phaseRemaining = duration;
            OnPhaseChanged?.Invoke(CurrentPhase);
            OnPhaseTimerChanged?.Invoke(_phaseRemaining);
        }

        private void EnsurePlayers()
        {
            if (_playerEntries.Count > 0)
            {
                if (string.IsNullOrEmpty(_gameSessionId))
                {
                    _gameSessionId = Guid.NewGuid().ToString("N");
                }
                return;
            }

            _playerEntries = new List<ArenaPlayerSpawnEntry>
            {
                new ArenaPlayerSpawnEntry { PlayerId = "P1", DisplayName = "Player 1", IsLocalControlled = true, Tint = new Color(0.92f, 0.32f, 0.32f, 1f), InitialScore = 0 },
                new ArenaPlayerSpawnEntry { PlayerId = "P2", DisplayName = "Player 2", IsLocalControlled = false, Tint = new Color(0.88f, 0.28f, 0.24f, 1f), InitialScore = 0 },
                new ArenaPlayerSpawnEntry { PlayerId = "P3", DisplayName = "Player 3", IsLocalControlled = false, Tint = new Color(0.95f, 0.42f, 0.36f, 1f), InitialScore = 0 },
                new ArenaPlayerSpawnEntry { PlayerId = "P4", DisplayName = "Player 4", IsLocalControlled = false, Tint = new Color(0.84f, 0.24f, 0.28f, 1f), InitialScore = 0 }
            };
            _gameSessionId = Guid.NewGuid().ToString("N");
        }

        private void BuildSessions()
        {
            _sessions.Clear();
            for (int i = 0; i < _playerEntries.Count; i++)
            {
                int initialScore = _playerEntries[i].InitialScore > 0
                    ? _playerEntries[i].InitialScore
                    : Mathf.Max(0, _standaloneStartingScore);
                var session = new ArenaPlayerSessionState
                {
                    PlayerId = string.IsNullOrWhiteSpace(_playerEntries[i].PlayerId) ? $"P{i + 1}" : _playerEntries[i].PlayerId,
                    DisplayName = string.IsNullOrWhiteSpace(_playerEntries[i].DisplayName) ? $"Player {i + 1}" : _playerEntries[i].DisplayName,
                    TeamId = 0,
                    StoredScore = initialScore,
                    IsReady = false,
                    IsAlive = true,
                    Placement = 0,
                    Tint = _playerEntries[i].Tint == default ? Color.white : _playerEntries[i].Tint,
                    PurchasedNodes = new List<ArenaNodeId>(),
                    ResolvedStats = ArenaSkillCatalog.ResolveStats(new List<ArenaNodeId>())
                };
                _sessions.Add(session);
            }
        }

        private void SelectMode()
        {
            int totalWeight = ArenaDesignValues.TeamModeWeight + ArenaDesignValues.FfaModeWeight;
            int roll = UnityEngine.Random.Range(0, totalWeight);
            CurrentMode = roll < ArenaDesignValues.TeamModeWeight ? ArenaModeType.Team2v2 : ArenaModeType.FreeForAll;
            OnModeChanged?.Invoke(CurrentMode);
        }

        private void AssignTeams()
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                _sessions[i].TeamId = CurrentMode == ArenaModeType.Team2v2 ? (i < 2 ? 0 : 1) : i;
            }
        }

        private void SpawnPlayersForCurrentMode()
        {
            ClearControllers();
            Vector3[] spawnPositions = ResolveSpawnPositions();
            for (int i = 0; i < _sessions.Count; i++)
            {
                GameObject playerObject = CreatePlayerObject(i, spawnPositions[i]);
                var controller = playerObject.GetComponent<ArenaPlayerController>();
                controller.Configure(_sessions[i], _playerEntries[i].IsLocalControlled);
                controller.ApplyColor(_sessions[i].Tint);
                controller.OnEliminated += HandlePlayerEliminated;
                _controllers.Add(controller);
            }
        }

        private GameObject CreatePlayerObject(int index, Vector3 spawnPosition)
        {
            if (_playerPrefab != null)
            {
                return Instantiate(_playerPrefab, spawnPosition, Quaternion.identity);
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"ArenaPlayer_{index + 1}";
            go.transform.position = spawnPosition;
            if (go.GetComponent<Rigidbody>() == null)
            {
                go.AddComponent<Rigidbody>();
            }

            if (go.GetComponent<ArenaPlayerController>() == null)
            {
                go.AddComponent<ArenaPlayerController>();
            }

            return go;
        }

        private Vector3[] ResolveSpawnPositions()
        {
            float r = Mathf.Max(3.8f, _spawnRadius);
            return new[]
            {
                _spawnCenter + new Vector3(-r, 0f, -r),
                _spawnCenter + new Vector3(r, 0f, -r),
                _spawnCenter + new Vector3(-r, 0f, r),
                _spawnCenter + new Vector3(r, 0f, r)
            };
        }

        private void SyncControllersFromSessions()
        {
            for (int i = 0; i < _controllers.Count && i < _sessions.Count; i++)
            {
                _controllers[i].Configure(_sessions[i], _playerEntries[i].IsLocalControlled);
                _controllers[i].ApplyColor(_sessions[i].Tint);
            }
        }

        private ArenaPlayerSessionState FindSession(string playerId)
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (_sessions[i].PlayerId == playerId)
                {
                    return _sessions[i];
                }
            }

            return null;
        }

        private ArenaPlayerController FindController(string playerId)
        {
            for (int i = 0; i < _controllers.Count; i++)
            {
                if (_controllers[i].PlayerId == playerId)
                {
                    return _controllers[i];
                }
            }

            return null;
        }

        private bool AreAllPlayersReady()
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (!_sessions[i].IsReady)
                {
                    return false;
                }
            }

            return true;
        }

        private void HandlePlayerEliminated(ArenaPlayerController controller)
        {
            var session = FindSession(controller.PlayerId);
            if (session == null || !session.IsAlive)
            {
                return;
            }

            session.IsAlive = false;
            _eliminationOrder.Add(session.PlayerId);
            SyncCameraTarget();
            OnSessionsChanged?.Invoke();

            if (CurrentMode == ArenaModeType.FreeForAll)
            {
                int aliveCount = CountAlivePlayers();
                if (aliveCount <= 1)
                {
                    FinishRound();
                }
            }
            else
            {
                if (IsTeamDefeated(0) || IsTeamDefeated(1))
                {
                    FinishRound();
                }
            }
        }

        private int CountAlivePlayers()
        {
            int count = 0;
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (_sessions[i].IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsTeamDefeated(int teamId)
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (_sessions[i].TeamId == teamId && _sessions[i].IsAlive)
                {
                    return false;
                }
            }

            return true;
        }

        private void AssignPlacements()
        {
            var orderedSessions = CurrentMode == ArenaModeType.FreeForAll
                ? BuildFfaPlacementOrder()
                : BuildTeamPlacementOrder();

            for (int i = 0; i < orderedSessions.Count; i++)
            {
                orderedSessions[i].Placement = i + 1;
            }
        }

        private List<ArenaPlayerSessionState> BuildFfaPlacementOrder()
        {
            var ordered = new List<ArenaPlayerSessionState>();
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (_sessions[i].IsAlive)
                {
                    ordered.Add(_sessions[i]);
                }
            }

            ordered.Sort((a, b) =>
            {
                var controllerA = FindController(a.PlayerId);
                var controllerB = FindController(b.PlayerId);
                float healthA = controllerA != null ? controllerA.CurrentHealth : 0f;
                float healthB = controllerB != null ? controllerB.CurrentHealth : 0f;
                return healthB.CompareTo(healthA);
            });

            for (int i = _eliminationOrder.Count - 1; i >= 0; i--)
            {
                var session = FindSession(_eliminationOrder[i]);
                if (session != null)
                {
                    ordered.Add(session);
                }
            }

            return ordered;
        }

        private List<ArenaPlayerSessionState> BuildTeamPlacementOrder()
        {
            int winningTeamId = ResolveWinningTeamId();
            int losingTeamId = winningTeamId == 0 ? 1 : 0;
            var ordered = new List<ArenaPlayerSessionState>();
            AppendTeamOrder(ordered, winningTeamId);
            AppendTeamOrder(ordered, losingTeamId);
            return ordered;
        }

        private int ResolveWinningTeamId()
        {
            int team0Alive = CountAliveByTeam(0);
            int team1Alive = CountAliveByTeam(1);
            if (team0Alive != team1Alive)
            {
                return team0Alive > team1Alive ? 0 : 1;
            }

            float team0Health = SumTeamHealth(0);
            float team1Health = SumTeamHealth(1);
            return team0Health >= team1Health ? 0 : 1;
        }

        private int CountAliveByTeam(int teamId)
        {
            int count = 0;
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (_sessions[i].TeamId == teamId && _sessions[i].IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private float SumTeamHealth(int teamId)
        {
            float total = 0f;
            for (int i = 0; i < _controllers.Count; i++)
            {
                if (_controllers[i].TeamId == teamId && _controllers[i].IsAlive)
                {
                    total += _controllers[i].CurrentHealth;
                }
            }

            return total;
        }

        private void AppendTeamOrder(List<ArenaPlayerSessionState> ordered, int teamId)
        {
            var alive = new List<ArenaPlayerSessionState>();
            var defeated = new List<ArenaPlayerSessionState>();
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (_sessions[i].TeamId != teamId)
                {
                    continue;
                }

                if (_sessions[i].IsAlive)
                {
                    alive.Add(_sessions[i]);
                }
                else
                {
                    defeated.Add(_sessions[i]);
                }
            }

            alive.Sort((a, b) =>
            {
                var controllerA = FindController(a.PlayerId);
                var controllerB = FindController(b.PlayerId);
                float healthA = controllerA != null ? controllerA.CurrentHealth : 0f;
                float healthB = controllerB != null ? controllerB.CurrentHealth : 0f;
                return healthB.CompareTo(healthA);
            });

            defeated.Sort((a, b) =>
            {
                int indexA = _eliminationOrder.IndexOf(a.PlayerId);
                int indexB = _eliminationOrder.IndexOf(b.PlayerId);
                return indexB.CompareTo(indexA);
            });

            ordered.AddRange(alive);
            ordered.AddRange(defeated);
        }

        private void ApplyPlacementScores()
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                _sessions[i].StoredScore += GetPlacementScoreStatic(_sessions[i].Placement);
                _sessions[i].IsReady = false;
            }
        }

        private void ClearControllers()
        {
            for (int i = 0; i < _controllers.Count; i++)
            {
                if (_controllers[i] != null)
                {
                    _controllers[i].OnEliminated -= HandlePlayerEliminated;
                    Destroy(_controllers[i].gameObject);
                }
            }

            _controllers.Clear();
        }

        private void UpdatePreparationBots()
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (_playerEntries[i].IsLocalControlled || _sessions[i].IsReady)
                {
                    continue;
                }

                if (Time.time < GetNextBotDecisionTime(_sessions[i].PlayerId))
                {
                    continue;
                }

                if (!TryPurchaseBotNode(i))
                {
                    SetPlayerReady(_sessions[i].PlayerId, true);
                }

                _botPrepDecisionAt[_sessions[i].PlayerId] = Time.time + UnityEngine.Random.Range(0.35f, 0.7f);
            }
        }

        private float GetNextBotDecisionTime(string playerId)
        {
            return _botPrepDecisionAt.TryGetValue(playerId, out float nextAt) ? nextAt : 0f;
        }

        private bool TryPurchaseBotNode(int playerIndex)
        {
            var session = _sessions[playerIndex];
            var plan = ResolveBotPlan(playerIndex);
            for (int i = 0; i < plan.Length; i++)
            {
                if (session.HasNode(plan[i]))
                {
                    continue;
                }

                if (ArenaSkillCatalog.CanPurchase(session, plan[i], out _))
                {
                    return TryPurchaseNode(session.PlayerId, plan[i], out _);
                }
            }

            return false;
        }

        private void SyncCameraTarget()
        {
            var target = GetPreferredFollowTarget();
            if (target == null)
            {
                return;
            }

            var cameraService = Camera.main != null
                ? Camera.main.GetComponent<PlayerFollowCameraService>()
                : null;
            if (cameraService == null)
            {
                cameraService = FindFirstObjectByType<PlayerFollowCameraService>();
            }

            cameraService?.Follow(target.transform);
        }

        private ArenaNodeId[] ResolveBotPlan(int playerIndex)
        {
            return playerIndex switch
            {
                1 => new[]
                {
                    ArenaNodeId.StrengthTrainingI,
                    ArenaNodeId.CarryHandling,
                    ArenaNodeId.HealthBoostI,
                    ArenaNodeId.DamageReduction,
                    ArenaNodeId.HeavyThrow
                },
                2 => new[]
                {
                    ArenaNodeId.JumpBoostI,
                    ArenaNodeId.AirControl,
                    ArenaNodeId.TempoI,
                    ArenaNodeId.ChargePrep,
                    ArenaNodeId.DoubleJump
                },
                _ => new[]
                {
                    ArenaNodeId.TempoI,
                    ArenaNodeId.ChargePrep,
                    ArenaNodeId.HealthBoostI,
                    ArenaNodeId.DamageReduction,
                    ArenaNodeId.BreathII
                }
            };
        }
    }
}
