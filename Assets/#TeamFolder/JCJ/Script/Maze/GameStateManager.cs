using System;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public class GameStateManager : MonoBehaviour, IGameStateService
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Services — Scene에 있는 컴포넌트 연결")]
        [SerializeField] private TimerService _timerService;
        [SerializeField] private RankService  _rankService;

        [Header("Settings")]
        [SerializeField] private float _gameDuration = 120f; // 초

        // ── 외부(UI 등)에서 인터페이스로 접근
        public ITimerService Timer => _timerService;
        public IRankService  Rank  => _rankService;

        public GameState CurrentState { get; private set; } = GameState.Waiting;
        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // 전원 도착 or 타임오버 → 자동 종료
            _rankService.OnAllFinished  += _ => ChangeState(GameState.Finished);
            _timerService.OnTimerExpired += () => ChangeState(GameState.Finished);
        }

        //  MazeManager에서 미로 생성 완료 후 호출
        public void StartGame() => ChangeState(GameState.Playing);

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
            Debug.Log($"[GameState] → {newState}");

            switch (newState)
            {
                case GameState.Playing:
                    _timerService.StartTimer(_gameDuration);
                    break;

                case GameState.Finished:
                    _timerService.StopTimer();
                    break;
            }
        }
    }
}
