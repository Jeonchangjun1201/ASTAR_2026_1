using System;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 게임의 Waiting, Countdown, Playing, Finished 상태를 전환하고 타이머·랭킹·점수 서비스를 묶어준다.
    /// </summary>
    public class GameStateManager : MonoBehaviour, IGameStateService
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

        public ITimerService     Timer     => _timerService;
        public IRankService      Rank      => _rankService;
        public ICountdownService Countdown => _countdownService;
        public IScoreService     Score     => _scoreService;

        public GameState CurrentState { get; private set; } = GameState.Waiting;
        public event Action<GameState> OnStateChanged;

        private bool _subscribed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            AutoBuildServices();
        }

        private void Start()
        {
            SubscribeServiceEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeServiceEvents();
            if (Instance == this) Instance = null;
        }

        private void AutoBuildServices()
        {
            // 씬에 연결되지 않은 서비스는 같은 오브젝트나 자식에서 찾고, 없으면 직접 추가한다.
            if (_timerService == null)     _timerService     = GetOrAdd<TimerService>();
            if (_rankService == null)      _rankService      = GetOrAdd<RankService>();
            if (_countdownService == null) _countdownService = GetOrAdd<CountdownService>();
            if (_scoreService == null)     _scoreService     = GetOrAdd<ScoreService>();
        }

        private T GetOrAdd<T>() where T : Component
        {
            var existing = GetComponent<T>();
            if (existing != null) return existing;
            var inChildren = GetComponentInChildren<T>(true);
            if (inChildren != null) return inChildren;
            return gameObject.AddComponent<T>();
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

        public void StartGame()
        {
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

        public void ResetToWaiting()
        {
            if (_countdownService != null) _countdownService.Cancel();
            if (_timerService != null) _timerService.ResetTimer();
            if (_rankService != null) _rankService.ResetRankings();
            if (_scoreService != null) _scoreService.Reset();
            ChangeState(GameState.Waiting);
        }

        public void ChangeState(GameState newState)
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
