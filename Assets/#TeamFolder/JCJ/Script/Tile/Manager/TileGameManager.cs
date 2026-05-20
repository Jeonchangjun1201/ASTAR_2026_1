using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _TeamFolder.JCJ.Script;
using _TeamFolder.JCJ.Script.Session;

// 타일 미니게임 라운드 흐름과 상태를 총괄하는 매니저.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 타일 미니게임 라운드를 총괄한다.
    /// 흐름: Waiting → Countdown(3-2-1-GO) → Playing ⇄ 컬러콜 이벤트 → Finished → (재시작).
    /// HUD·오디오·카메라·ColorCallDirector는 연결 안 되어 있으면 자동 추가 — 씬에는 이 컴포넌트 + TileBoard + PlayerSpawnManager만 두면 된다.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class TileGameManager : MonoBehaviour, ITileRoundGateway
    {
        public static TileGameManager Instance { get; private set; }

        [Header("참조")]
        [SerializeField] private TileBoard             tileBoard;
        [SerializeField] private PlayerSpawnManager    spawnManager;
        [SerializeField] private GameConfig            gameConfig;

        [Header("설정")]
        [Tooltip("스폰할 플레이어 수. 기본 1(단일). 네트워크 붙이면 4 등으로 올린다.")]
        [SerializeField] private int playerCount = 1;

        [Header("자동 생성")]
        [Tooltip("TileHUD 없으면 자동 추가.")]
        [SerializeField] private bool buildHUD = true;
        [Tooltip("TileAudio 없으면 자동 추가.")]
        [SerializeField] private bool buildAudio = true;
        [Tooltip("필요 시 메인 카메라에 TileCameraFollow 자동 추가.")]
        [SerializeField] private bool buildCamera = true;
        [Tooltip("ColorCallDirector 없으면 자동 추가.")]
        [SerializeField] private bool buildColorCall = true;
        [SerializeField] private CountdownUI countdownUI;

        // 서비스(자동 해석).
        private TileHUD             _hud;
        private TileCameraFollow    _camera;
        private ColorCallDirector   _colorCall;

        // 라운드 상태.
        private GameState                         _state = GameState.Waiting;
        private readonly List<PlayerController>   _alivePlayers   = new();
        private readonly List<PlayerController>   _allPlayers     = new();
        private readonly Dictionary<PlayerController, int> _scores = new();
        private float                             _timerRemaining;
        private int                               _maxLives;

        public GameState  State  => _state;
        public GameConfig Config => gameConfig;
        public event System.Action RoundStartRequested;
        public event System.Action RoundRestartRequested;
        public event System.Action<string> RoundEndRequested;
        public event System.Action<string> RespawnRequested;
        public event System.Action<string> FallResolutionRequested;

        // ── Unity ───────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            JcjClientSessionHub.RegisterTileRound(this);
        }

        private void OnDestroy()
        {
            // 매니저가 플레이어보다 먼저 파괴될 수 있어(씬 언로드 순서) 이벤트 구독을 모두 해제한다.
            foreach (var p in _allPlayers)
            {
                if (p == null) continue;
                p.OnFell       -= HandleFell;
                p.OnEliminated -= HandleEliminated;
                p.FallResolutionRequested -= HandleFallResolutionRequested;
            }
            if (Instance == this)
            {
                JcjClientSessionHub.UnregisterTileRound(this);
                Instance = null;
            }
        }

        private void Start()
        {
            if (JcjRuntimeAuthority.UseLocalSimulation) BeginRound();
            else RoundStartRequested?.Invoke();
        }

        private void Update()
        {
            if (!JcjRuntimeAuthority.UseLocalSimulation) return;
            if (_state != GameState.Playing) return;
            _timerRemaining -= Time.deltaTime;
            TickScores();
            _hud?.SetTimer(_timerRemaining);
            if (_timerRemaining <= 0f) EndRound(cause: "TIMER");
        }

        // ── 라운드 흐름 ─────────────────────────────
        public void BeginRound()
        {
            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                RoundStartRequested?.Invoke();
                return;
            }
            BeginRoundInternal();
        }

        public void ApplyAuthoritativeRoundStart()
        {
            BeginRoundInternal();
        }

        private void BeginRoundInternal()
        {
            // Tile 라운드의 시작점이다.
            // 서버를 붙이면 이 함수는 서버/호스트에서 실행하고, 생성 결과만 클라이언트에 동기화하는 구조가 가장 단순하다.
            if (gameConfig == null)
            {
                Debug.LogError("[TileGameManager] GameConfig is not assigned.");
                return;
            }
            if (tileBoard == null)
            {
                Debug.LogError("[TileGameManager] TileBoard reference is missing.");
                return;
            }
            if (spawnManager == null)
            {
                Debug.LogError("[TileGameManager] PlayerSpawnManager reference is missing.");
                return;
            }

            ResolveServices();

            _state = GameState.Waiting;
            _alivePlayers.Clear();
            _allPlayers.Clear();
            _scores.Clear();
            _timerRemaining = gameConfig.roundDuration;
            _maxLives = gameConfig.playerLives;

            // TileBoard는 ScriptableObject 수치(GameConfig)를 기준으로 모든 타일을 런타임 생성한다.
            // 멀티에서는 이 생성 결과가 모든 클라이언트에서 같아야 하므로 서버 seed/타일 ID 동기화가 필요하다.
            ITileFactory factory = new TileFactory(gameConfig, tileBoard);
            tileBoard.Initialize(factory, gameConfig);

            // 플레이어 스폰.
            List<PlayerController> players = spawnManager.SpawnPlayers(playerCount, tileBoard, gameConfig);
            foreach (var p in players)
            {
                p.OnFell        += HandleFell;
                p.OnEliminated  += HandleEliminated;
                p.FallResolutionRequested += HandleFallResolutionRequested;
                _alivePlayers.Add(p);
                _allPlayers.Add(p);
                _scores[p] = 0;
            }

            // 카메라에 타깃 전달.
            if (_camera != null)
            {
                var xforms = new List<Transform>();
                foreach (var p in _allPlayers) xforms.Add(p.transform);
                _camera.RegisterTargets(xforms);
            }

            RefreshHudStatic();

            StartCoroutine(CountdownAndPlay());
        }

        private IEnumerator CountdownAndPlay()
        {
            // 카운트다운 동안에는 플레이어 입력을 잠근다.
            // 서버 연동 시 카운트다운 시작/GO 타이밍은 모든 클라이언트가 같은 시각에 보도록 RPC로 맞추는 것이 좋다.
            _state = GameState.Countdown;
            GameplayCursor.SetLocked(false);

            countdownUI ??= FindFirstObjectByType<CountdownUI>();
            int seconds = Mathf.Max(0, gameConfig.countdownSeconds);
            if (countdownUI != null)
            {
                void Tick(int _) => TileAudio.PlayStatic(TileSfx.CountdownTick, 0.8f, 1f);
                void Go() => TileAudio.PlayStatic(TileSfx.Go, 1f, 1f);
                countdownUI.OnTick += Tick;
                countdownUI.OnGo += Go;
                yield return countdownUI.PlayRoutine(seconds);
                countdownUI.OnTick -= Tick;
                countdownUI.OnGo -= Go;
            }
            else
            {
                for (int i = seconds; i > 0; i--)
                {
                    TileAudio.PlayStatic(TileSfx.CountdownTick, 0.8f, 1f);
                    yield return new WaitForSeconds(1f);
                }
                TileAudio.PlayStatic(TileSfx.Go, 1f, 1f);
                yield return new WaitForSeconds(0.6f);
            }

            // 입력 잠금 해제.
            foreach (var p in _allPlayers)
                if (p != null) p.InputLocked = false;

            _state = GameState.Playing;
            _timerRemaining = gameConfig.roundDuration;

            // 커서 잠금 — TileCameraFollow 마우스 요가 동작. 결과/카운트다운은 해제, 실제 플레이 중만 잠금.
            GameplayCursor.SetLocked(true);

            if (_colorCall != null)
            {
                // ColorCall은 타일 서바이벌의 핵심 규칙이다.
                // 안전색 선택과 타일 드롭은 반드시 한 곳(서버/호스트)에서 결정해야 클라별 결과가 갈라지지 않는다.
                _colorCall.Inject(gameConfig, tileBoard);
                _colorCall.BeginLoop();
            }
        }

        // ── 점수 ─────────────────────────────────────
        private float _scoreTickAccum;
        private void TickScores()
        {
            // 생존 시간 점수를 초 단위로 누적하는 로직이다.
            // 서버 게임에서는 클라이언트 프레임 시간이 아니라 서버 틱/라운드 시간 기준 점수로 바꾸는 편이 안전하다.
            _scoreTickAccum += Time.deltaTime;
            while (_scoreTickAccum >= 1f)
            {
                _scoreTickAccum -= 1f;
                foreach (var p in _alivePlayers)
                    if (p != null) _scores[p] = _scores[p] + gameConfig.scorePerSecondAlive;
            }
        }

        private void AwardColorCallSurvivors()
        {
            // 컬러콜 생존 보너스 지급 지점이다.
            // 서버가 안전색 판정까지 맡는다면 보너스 부여도 같은 이벤트 흐름에서 확정하는 것이 자연스럽다.
            foreach (var p in _alivePlayers)
            {
                if (p == null) continue;
                if (!p.SurvivedLastColorCall) continue;
                _scores[p] = _scores[p] + gameConfig.scorePerColorCallSurvived;
                p.SurvivedLastColorCall = false;
            }
        }

        // ── 플레이어 콜백 ────────────────────────────
        private void HandleFell(PlayerController p)
        {
            if (_state != GameState.Playing) return;
            // 아직 목숨 남음 — 낙하 피드백으로 카메라 쉐이크.
            _camera?.Shake(0.35f, 0.4f);
            RefreshHudDynamic();
        }

        private void HandleFallResolutionRequested(PlayerController player)
        {
            if (player == null) return;
            var identity = RuntimePlayerIdentity.Find(player);
            FallResolutionRequested?.Invoke(identity != null ? identity.PlayerId : player.gameObject.name);
        }

        private void HandleEliminated(PlayerController p)
        {
            // 플레이어가 목숨을 모두 잃었을 때 호출된다.
            // 서버 연동 시 _alivePlayers, FinalRank, 점수 보너스는 서버 권위 상태로 관리하는 것을 권장한다.
            if (!JcjRuntimeAuthority.UseLocalSimulation) return;
            if (_state != GameState.Playing && _state != GameState.Countdown) return;
            if (!_alivePlayers.Remove(p)) return;

            // 순위: 먼저 죽을수록 낮은 순위. _alivePlayers 갱신 후 rank = 남은 생존자 수 + 1.
            // 예: 4명 중 첫 탈락 → 제거 후 alive=3 → 순위 4.
            if (p.FinalRank == 0) p.FinalRank = _alivePlayers.Count + 1;

            Debug.Log($"[TileGameManager] {p.name} eliminated. Rank={p.FinalRank}  Alive={_alivePlayers.Count}");
            TileAudio.PlayStatic(TileSfx.EliminatePeer, 0.8f, 1f);
            RefreshHudDynamic();

            // 카메라 인수:
            //  - 생존자 있음 → 첫 생존자로 전환 + 짧은 쉐이크.
            //  - 모두 사망 → 판 전체를 보는 오버뷰(시체에 카메라가 붙어 떨리는 것 방지).
            if (_camera != null)
            {
                if (_alivePlayers.Count > 0)
                {
                    var next = _alivePlayers[0];
                    if (next != null) _camera.RegisterTarget(next.transform);
                    _camera.Shake(0.25f, 0.2f);
                }
                else if (tileBoard != null)
                {
                    _camera.GoToOverview(tileBoard.GetBoardCenter());
                }
            }

            // 멀티: 생존자 1명이면 즉시 종료(승자 확정).
            if (_alivePlayers.Count == 1 && _allPlayers.Count > 1)
            {
                var survivor = _alivePlayers[0];
                if (survivor != null)
                {
                    _scores[survivor] = _scores[survivor] + gameConfig.scoreLastSurvivor;
                    survivor.FinalRank = 1;
                }
                EndRound(cause: "LAST_STANDING");
                return;
            }
            // 싱글이거나 멀티에서 전멸: 아무도 없으면 종료.
            if (_alivePlayers.Count == 0)
                EndRound(cause: "ALL_ELIMINATED");
        }

        // ── 종료 ─────────────────────────────────────
        private void EndRound(string cause)
        {
            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                RoundEndRequested?.Invoke(cause);
                return;
            }
            EndRoundInternal(cause);
        }

        public void ApplyAuthoritativeRoundEnd(string cause)
        {
            EndRoundInternal(cause);
        }

        private void EndRoundInternal(string cause)
        {
            // 라운드 종료 지점이다. 결과 UI 표시 전에 순위와 점수를 확정한다.
            // 네트워크에서는 cause와 최종 ranking을 서버가 확정해서 모든 클라이언트에 보내야 한다.
            if (_state == GameState.Finished) return;
            _state = GameState.Finished;

            // 타이머로 끝나면 생존자 보너스.
            if (cause == "TIMER")
            {
                foreach (var p in _alivePlayers)
                    if (p != null) _scores[p] = _scores[p] + gameConfig.scoreTimerSurvivor;
            }

            // 라운드 끝에 아직 순위 없는 생존자 — 탈락자 위 포디움 자리를 점수로 나눔(동점은 점수로 결정).
            AssignSurvivorRanks();

            _colorCall?.EndLoop();

            // 입력 다시 잠금(이후 관전 모드).
            foreach (var p in _allPlayers)
                if (p != null) p.InputLocked = true;

            GameplayCursor.SetLocked(false);

            var ranking = BuildRanking();
            if (TileAudio.Instance != null) TileAudio.Instance.DuckMusic(4f);
            TileAudio.PlayStatic(TileSfx.Fanfare, 1f, 1f);
            _hud?.ShowResults(ranking);
        }

        /// <summary>
        /// 라운드 종료 시 생존자 순위. 점수 높은 순으로 정렬해 아직 순위 없는 플레이어에 1,2,3… 부여.
        /// </summary>
        private void AssignSurvivorRanks()
        {
            var survivors = new List<PlayerController>();
            foreach (var p in _alivePlayers) if (p != null) survivors.Add(p);
            survivors.Sort((a, b) =>
            {
                int sa = _scores.TryGetValue(a, out var va) ? va : 0;
                int sb = _scores.TryGetValue(b, out var vb) ? vb : 0;
                return sb.CompareTo(sa);
            });
            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i].FinalRank == 0) survivors[i].FinalRank = i + 1;
            }
        }

        private IReadOnlyList<string> BuildRanking()
        {
            // FinalRank 오름차순(1등 먼저). 미배정(0)은 맨 아래.
            var sorted = new List<PlayerController>(_allPlayers);
            sorted.Sort((a, b) =>
            {
                int ra = a != null && a.FinalRank > 0 ? a.FinalRank : int.MaxValue;
                int rb = b != null && b.FinalRank > 0 ? b.FinalRank : int.MaxValue;
                if (ra != rb) return ra.CompareTo(rb);
                int sa = a != null && _scores.TryGetValue(a, out var va) ? va : 0;
                int sb = b != null && _scores.TryGetValue(b, out var vb) ? vb : 0;
                return sb.CompareTo(sa);
            });

            var medals = new[] { "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th" };
            var lines = new List<string>();
            for (int i = 0; i < sorted.Count; i++)
            {
                var p = sorted[i];
                if (p == null) continue;
                int rank = p.FinalRank > 0 ? p.FinalRank : (i + 1);
                int score = _scores.TryGetValue(p, out var v) ? v : 0;
                string medal = (rank - 1) < medals.Length ? medals[rank - 1] : $"#{rank}";
                var identity = RuntimePlayerIdentity.Find(p);
                string name = identity != null
                    ? identity.DisplayName
                    : (string.IsNullOrEmpty(p.name) ? $"Player {p.PlayerIndex + 1}" : p.name);
                lines.Add($"{medal}   {name}   —   {score} pts");
            }
            return lines;
        }

        public void RestartRound()
        {
            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                RoundRestartRequested?.Invoke();
                return;
            }
            RestartRoundInternal();
        }

        public void ApplyAuthoritativeRestart()
        {
            RestartRoundInternal();
        }

        private void RestartRoundInternal()
        {
            // 로컬 테스트용 재시작이다.
            // 서버 연동 시에는 모든 클라이언트가 같은 타이밍에 기존 오브젝트를 정리하고 새 라운드를 받아야 한다.
            // 기존 플레이어 정리.
            foreach (var p in _allPlayers)
                if (p != null) Destroy(p.gameObject);

            // 타일 정리.
            if (tileBoard != null)
            {
                foreach (Transform child in tileBoard.transform)
                    Destroy(child.gameObject);
            }

            _hud?.HideResults();
            countdownUI?.Cancel();

            BeginRoundInternal();
        }

        // ── 리스폰(플레이어 SetActive(false) 후에도 코루틴 유지하려고 매니저 소유) ──
        public void RequestRespawn(PlayerController player)
        {
            // 플레이어가 떨어졌지만 목숨이 남아 있을 때 매니저가 리스폰 타이밍을 관리한다.
            // 플레이어 오브젝트가 비활성화되어도 코루틴이 끊기지 않도록 매니저 소유로 둔다.
            if (player == null) return;
            if (_state != GameState.Playing && _state != GameState.Countdown) return;
            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                var identity = RuntimePlayerIdentity.Find(player);
                RespawnRequested?.Invoke(identity != null ? identity.PlayerId : player.gameObject.name);
                return;
            }
            StartCoroutine(RespawnCoroutine(player));
        }

        public void ApplyAuthoritativeRespawn(string playerId, Vector3 target, float invuln)
        {
            var player = FindPlayerById(playerId);
            if (player == null) return;
            if (_state == GameState.Finished) return;
            player.CompleteRespawn(target, invuln);
        }

        private IEnumerator RespawnCoroutine(PlayerController player)
        {
            float delay = gameConfig != null ? gameConfig.respawnDelay : 1.2f;
            yield return new WaitForSeconds(delay);

            if (player == null) yield break;
            if (player.IsEliminated) yield break;
            if (_state == GameState.Finished) yield break;

            Vector3 target = GetSafeRespawnPosition();
            float invuln = gameConfig != null ? gameConfig.respawnInvuln : 1.5f;
            player.CompleteRespawn(target, invuln);
        }

        // ── 리스폰 위치 탐색 ─────────────────────────
        public Vector3 GetSafeRespawnPosition()
        {
            if (tileBoard == null) return Vector3.zero;
            var candidates = tileBoard.GetAliveTileCenters();
            if (candidates.Count == 0) return tileBoard.GetBoardCenter() + Vector3.up * 3f;

            // 다른 플레이어와 최대한 멀리 — 스폰 직후 충돌 완화.
            Vector3 bestPos = candidates[0];
            float bestScore = float.MinValue;
            foreach (var pos in candidates)
            {
                float worstNear = float.MaxValue;
                foreach (var peer in _alivePlayers)
                {
                    if (peer == null || !peer.gameObject.activeInHierarchy) continue;
                    float d = Vector3.Distance(peer.transform.position, pos);
                    if (d < worstNear) worstNear = d;
                }
                if (worstNear > bestScore) { bestScore = worstNear; bestPos = pos; }
            }
            return bestPos;
        }

        // ── HUD 갱신 헬퍼 ───────────────────────────
        private void RefreshHudStatic()
        {
            if (_hud == null) return;
            _hud.SetTimer(_timerRemaining);
            _hud.SetAlive(_alivePlayers.Count, _allPlayers.Count);
            var local = ResolveLocalPlayer();
            if (local != null)
                _hud.SetLives(local.LivesRemaining, _maxLives);
        }

        private void RefreshHudDynamic()
        {
            if (_hud == null) return;
            _hud.SetAlive(_alivePlayers.Count, _allPlayers.Count);
            PlayerController local = ResolveLocalPlayer();
            if (local != null) _hud.SetLives(local.LivesRemaining, _maxLives);
        }

        private PlayerController ResolveLocalPlayer()
        {
            for (int i = 0; i < _allPlayers.Count; i++)
            {
                var player = _allPlayers[i];
                if (player == null) continue;
                var identity = RuntimePlayerIdentity.Find(player);
                if (identity != null && identity.IsLocalOwned) return player;
                if (player.IsLocalControlled) return player;
            }

            return _allPlayers.Count > 0 ? _allPlayers[0] : null;
        }

        private PlayerController FindPlayerById(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return null;
            for (int i = 0; i < _allPlayers.Count; i++)
            {
                var player = _allPlayers[i];
                if (player == null) continue;
                var identity = RuntimePlayerIdentity.Find(player);
                if (identity != null && string.Equals(identity.PlayerId, playerId, System.StringComparison.OrdinalIgnoreCase))
                    return player;
            }

            return null;
        }

        // ── 서비스 해석 ─────────────────────────────
        private void ResolveServices()
        {
            countdownUI ??= FindFirstObjectByType<CountdownUI>();

            if (buildHUD)
            {
                _hud = SceneComponentResolver.FindOrCreate<TileHUD>(transform, "TileHUD");
            }

            if (buildAudio && TileAudio.Instance == null)
                gameObject.AddComponent<TileAudio>();

            if (buildCamera)
            {
                _camera = SceneComponentResolver.GetOrAddOnMainCamera<TileCameraFollow>("TileMainCamera");
            }

            if (buildColorCall)
            {
                _colorCall = SceneComponentResolver.FindOrCreate<ColorCallDirector>(transform, "ColorCallDirector");
                _colorCall.Inject(gameConfig, tileBoard);
                _colorCall.OnAnnounced   -= HandleColorCallAnnounced;
                _colorCall.OnAnnounced   += HandleColorCallAnnounced;
                _colorCall.OnDropped     -= HandleColorCallDropped;
                _colorCall.OnDropped     += HandleColorCallDropped;
                _colorCall.OnEventEnded  -= HandleColorCallEnded;
                _colorCall.OnEventEnded  += HandleColorCallEnded;
            }
        }

        // ── 컬러콜 콜백 ─────────────────────────────
        private void HandleColorCallAnnounced(TileColor safe, float warn)
        {
            _hud?.ShowColorCallAnnounce(safe, warn);
            // 이벤트 끝날 때 점수 줄 생존 의도만 기록.
            foreach (var p in _alivePlayers)
                if (p != null) p.SurvivedLastColorCall = false;
        }

        private void HandleColorCallDropped(TileColor safe, int dropped)
        {
            _camera?.Shake(0.5f, 0.6f);
        }

        private void HandleColorCallEnded()
        {
            _hud?.HideColorCall();
            // 생존자 표시 후 점수 부여.
            foreach (var p in _alivePlayers)
                if (p != null && !p.IsEliminated) p.SurvivedLastColorCall = true;
            AwardColorCallSurvivors();
        }
    }
}
