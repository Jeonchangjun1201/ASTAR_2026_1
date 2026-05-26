using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using _TeamFolder.PYH._02.Scripts.Util;
using Assets._TeamFolder.PYH._02.Scripts.UI.Event;
using System;
using System.Collections;
using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{
    public class JHJPaintingGameTimerManager : MonoSingleton<JHJPaintingGameTimerManager>
    {
        [SerializeField] private float _readyTimer = 4f;
        [SerializeField] private int _gameTimer = 60;

        public bool IsGamePlaying { get; private set; } = false;

        public event Action OnGameStarted;
        public event Action<float> OnReadyTimeUpdated;
        public event Action<float> OnTimeUpdated;
        public event Action OnGameEnded;

        private void Awake()
        {
            AStarEventBus.Subscribe<DigitalClockUiEndEvent>(GameEndEventHandler);
        }
        private void OnDestroy()
        {
            AStarEventBus.Unsubscribe<DigitalClockUiEndEvent>(GameEndEventHandler);
        }

        private void Start()
        {
            AStarEventBus.Publish(new DigitalClockUiTimeSetEvent(_gameTimer));
            StartCoroutine(GameFlowRoutine());
        }

        private IEnumerator GameFlowRoutine()
        {
            AStarEventBus.Publish(new CountdownUiEvent());
            IsGamePlaying = false;

            // 1. 대기 시간 카운트다운 (4초)
            float readyTime = _readyTimer;
            while (readyTime > 0)
            {
                readyTime -= Time.deltaTime;
                OnReadyTimeUpdated?.Invoke(readyTime);
                yield return null;
            }

            // 2. 게임 시작
            IsGamePlaying = true;
            OnGameStarted?.Invoke();
            AStarEventBus.Publish(new DigitalClockUiStartEvent());

            // 3. 본 게임 시간 카운트다운 (주석을 풀었습니다! 진짜 60초를 셉니다)
            float currentTime = _gameTimer;
            while (currentTime > 0)
            {
                currentTime -= Time.deltaTime; // 매 프레임마다 시간을 깎음
                OnTimeUpdated?.Invoke(currentTime);
                yield return null;
            }

            // 4. 시간이 0이 되면 게임 종료 이벤트를 쏴서 모두에게 알림
            AStarEventBus.Publish(new DigitalClockUiEndEvent());
        }

        private void GameEndEventHandler(DigitalClockUiEndEvent @event)
        {
            // 이벤트가 도착하면 여기가 실행되면서 게임이 확실히 끝남!
            IsGamePlaying = false;
            OnTimeUpdated?.Invoke(0);
            OnGameEnded?.Invoke();
        }
    }
}