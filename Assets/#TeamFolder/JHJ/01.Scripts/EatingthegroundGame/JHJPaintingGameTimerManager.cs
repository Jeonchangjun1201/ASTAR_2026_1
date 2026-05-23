using PYH.Util;
using System;   
using System.Collections;
using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{
    public class JHJPaintingGameTimerManager : MonoSingleton<JHJPaintingGameTimerManager>
    {
        [SerializeField] private float _readyTimer = 3f;
        [SerializeField] private float _gameTimer = 60f;

        public event Action OnGameStarted;
        public event Action<float> OnReadyTimeUpdated;
        public event Action<float> OnTimeUpdated;
        public event Action OnGameEnded;

        private void Start()
        {
            StartCoroutine(GameFlowRoutine());
        }

        private IEnumerator GameFlowRoutine()
        {
            //  대기 시간 카운트다운
            float readyTime = _readyTimer;
            while (readyTime > 0)
            {
                readyTime -= Time.deltaTime;
                OnReadyTimeUpdated?.Invoke(readyTime);
                yield return null;
            }

            // 게임 시작 액션 날리고
            OnGameStarted?.Invoke();

            // 본 게임 시간 카운트다운
            float currentTime = _gameTimer;
            while (currentTime > 0)
            {
                currentTime -= Time.deltaTime;
                OnTimeUpdated?.Invoke(currentTime);
                yield return null;
            }

            // 게임 종료 처리 액션 날리고
            OnTimeUpdated?.Invoke(0);
            OnGameEnded?.Invoke();
        }
    }
}