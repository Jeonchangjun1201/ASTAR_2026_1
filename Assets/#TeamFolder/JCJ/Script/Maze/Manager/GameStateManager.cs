using System;
using UnityEngine;
using _TeamFolder.JCJ.Script.Session;

// 상태 전환, 타이머, 점수, 랭킹 흐름을 총괄하는 매니저.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 게임의 Waiting, Countdown, Playing, Finished 상태를 전환하고 타이머·랭킹·점수 서비스를 묶어준다.
    /// </summary>
    public class GameStateManager : MonoBehaviour, IGameStateServerGateway
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Services — Scene에 있는 컴포넌트 연결")]
        [SerializeField] private TimerService _timerService;
        [SerializeField] private RankService _rankService;
        [SerializeField] private CountdownService _countdownService;
        [SerializeField] private ScoreService _scoreService;

        [Header("설정")]
        [SerializeField] private float _gameDuration = 120f;
        [SerializeField] private int _countdownSeconds = 3;

        [Header("미로 완주 순위 (RankService 전용)")]
        [SerializeField] private ScoreConfig _mazeScoreConfig;
        [SerializeField] private int _mazeTotalPlayers = 4;
        [SerializeField] private int _mazePodiumSize = 3;

        public ITimerService     Timer     => _timerService;
        public IRankService      Rank      => _rankService;
        public ICountdownService Countdown => _countdownService;
        public IScoreService     Score     => _scoreService;

        public GameState CurrentState { get; private set; } = GameState.Waiting;
        public event Action<GameState> OnStateChanged;
        public event Action StartGameRequested;
        public event Action ResetRequested;
        public event Action<GameState> StateChangeRequested;

        private bool _subscribed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            JcjClientSessionHub.RegisterGameState(this);
            AutoBuildServices();
        }

        private void Start()
        {
            SubscribeServiceEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeServiceEvents();
            if (Instance == this)
            {
                JcjClientSessionHub.UnregisterGameState(this);
                Instance = null;
            }
        }

        private void AutoBuildServices()
        {
            var scoreRank = MatchScoreRankManager.EnsureExists();
            if (_scoreService == null && scoreRank?.Score is ScoreService scoreConcrete)
                _scoreService = scoreConcrete;

            if (_timerService == null)     _timerService     = SceneComponentResolver.GetOrAdd<TimerService>(this);
            if (_rankService == null)      _rankService      = SceneComponentResolver.GetOrAdd<RankService>(this);
            if (_countdownService == null) _countdownService = SceneComponentResolver.GetOrAdd<CountdownService>(this);
            if (_scoreService == null)     _scoreService     = SceneComponentResolver.FindOrCreate<ScoreService>(null, "ScoreService");

            ApplyMazeRankConfig();
        }

        private void ApplyMazeRankConfig()
        {
            if (_rankService == null) return;
            _rankService.Configure(_mazeScoreConfig, _mazeTotalPlayers, _mazePodiumSize);
        }

        private void SubscribeServiceEvents()
        {
            if (_subscribed) return;

            if (_rankService != null)
                _rankService.OnAllFinished += HandleAllFinished;
            if (_timerService != null)
                _timerService.OnTimerExpired += HandleTimerExpired;
            if (_countdownService != null)
                _countdownService.OnGo += HandleCountdownEnded;

            _subscribed = true;
        }

        private void UnsubscribeServiceEvents()
        {
            if (!_subscribed) return;

            if (_rankService != null)
                _rankService.OnAllFinished -= HandleAllFinished;
            if (_timerService != null)
                _timerService.OnTimerExpired -= HandleTimerExpired;
            if (_countdownService != null)
                _countdownService.OnGo -= HandleCountdownEnded;

            _subscribed = false;
        }

        // 미로 라운드 시작 요청 진입점이다.
        // 서버를 붙이면 호스트/서버가 시작을 확정하고 각 클라이언트는 그 상태 전환만 반영하면 된다.
        public void StartGame()
        {
            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                StartGameRequested?.Invoke();
                return;
            }

            // 카운트다운이 설정되어 있으면 GO 이벤트 후 Playing으로 넘어가고, 아니면 즉시 시작한다.
            if (_countdownService != null && _countdownSeconds > 0)
            {
                ChangeState(GameState.Countdown);
                _countdownService.Begin(_countdownSeconds);
            }
            else
            {
                ChangeState(GameState.Playing);
            }
        }

        // 라운드 상태와 관련 서비스 데이터를 처음 대기 상태로 되돌린다.
        // 세션 재시작이나 방 재입장 처리 때 초기화 순서를 찾기 좋은 지점이다.
        public void ResetToWaiting()
        {
            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                ResetRequested?.Invoke();
                return;
            }

            if (_countdownService != null) _countdownService.Cancel();
            if (_timerService != null) _timerService.ResetTimer();
            if (_rankService != null) _rankService.ResetRankings();
            MatchScoreRankManager.Instance?.ResetScores();
            if (_scoreService != null) _scoreService.Reset();
            ChangeStateInternal(GameState.Waiting);
        }

        // 모든 서브시스템이 공통으로 보는 상태 값을 바꾸는 중심 메서드다.
        // 서버 연동 시에는 이 메서드가 로컬에서 상태를 만들기보다 서버 상태 스냅샷을 반영하는 창구가 된다.
        public void ChangeState(GameState newState)
        {
            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                StateChangeRequested?.Invoke(newState);
                return;
            }
            ChangeStateInternal(newState);
        }

        public void ApplyAuthoritativeState(GameState newState)
        {
            ChangeStateInternal(newState);
        }

        private void ChangeStateInternal(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
            Debug.Log($"[GameState] → {newState}");

            // 상태 진입 시 필요한 서비스 동작만 여기서 실행해 상태 전환 규칙을 한 곳에 모은다.
            switch (newState)
            {
                case GameState.Playing:
                    if (_timerService != null) _timerService.StartTimer(_gameDuration);
                    break;

                case GameState.Finished:
                    if (_timerService != null) _timerService.StopTimer();
                    if (_countdownService != null) _countdownService.Cancel();
                    break;
            }
        }

        private void HandleAllFinished(System.Collections.Generic.List<PlayerRankData> _) => ChangeState(GameState.Finished);
        private void HandleTimerExpired()
        {
            // 랭크 서비스를 확정시켜 HUD/포디움이 OnAllFinished를 받게 한다.
            // 인터페이스 경로로 두면 다른 구현(예: 네트워크 랭크 서비스)도 타임아웃 시 동일하게 확정된다.
            if (_rankService != null) ((IRankService)_rankService).FinalizeNow();
            ChangeState(GameState.Finished);
        }
        private void HandleCountdownEnded() => ChangeState(GameState.Playing);
    }
}
