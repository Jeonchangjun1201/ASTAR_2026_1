using PYH.Util;
using System;
using System.Collections;
using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{
    public class JHJPaintingGameTimerManager : MonoSingleton<JHJPaintingGameTimerManager>
    {
        [Header("시간 설정")]
        [SerializeField] private float _readyTimer = 3f; //대기 시간
        [SerializeField] private float _gameTimer = 60f; //전체 게임 시간

        public event Action OnGameStarted;           // 게임 시작 시
        public event Action<float> OnTimeUpdated;    // 남은시간 UI에 전달
        public event Action OnGameEnded;             //게임 끝낫을 떄 이벤트

        private void Start()
        {
            StartCoroutine(GameFlowRoutine());
        }

        private IEnumerator GameFlowRoutine()
        {
            Debug.Log("게임 시작 대기 중");
            yield return new WaitForSeconds(_readyTimer);
            Debug.Log("시작");
            OnGameStarted?.Invoke();

            float currentTime = _gameTimer;
            while (currentTime > 0)
            {
                currentTime -= Time.deltaTime;
                OnTimeUpdated?.Invoke(currentTime); 
                yield return null; 
            }
            OnTimeUpdated?.Invoke(0);
            GameEnd();
        }

        private void GameEnd()
        {
            Debug.Log("게임 종료 서버에 정보(결과) 보내기");

            //게임 종료 신호 보내는 메서드(서버 연동 스크립트,UI 매니저 등등 이 정보를 받음)
            OnGameEnded?.Invoke();
        }
    }
}