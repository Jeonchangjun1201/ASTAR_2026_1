using PYH.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BFS
{
    public class FSStageManager : MonoBehaviour                                      // Stage manager for Four Sides game 스테이지를 관리하는 관리자
    {
        [SerializeField] private FSStageListSO stageList;                            // StageList // 스테이지 리스트
        public event Action<FSCameraView> OnCameraViewChange;                        // Action to change camera view // 카메라 시점 변경하는 액션
        public event Action OnPlateQueue;                                            // Action to add plates to queue(and change color screen with game manager) // 발판을 큐에 넣는(그리고 게임 매니저로 모니터 화면을 변경하는) 액션
        public event Action OnScreenReset;                                           // Action to reset monitor screen // 모니터 화면을 초기화하는 액션
        public event Action<float> OnPlateDequeue;                                   // Action to remove/deactivate plates // 발판을 활성화/비활성화 하는 액션
        public event Action<int> OnCountDown;
        public event Action<string> OnGameEnd;

        private List<FSPlayer> _playerList;
        private FSUIManager _uiManager;
        private FSGameOverManager _gameOverManager;
        private float _colorDelay;
        private int _colorCount;
        private int _currentStage = 0;
        private float _plateDisableDuration;
        private int _countDownTime;
        private bool _inGame;

        public float ColorDelay => _colorDelay;
        public float PlateDisableDuration => _plateDisableDuration;
        public void GameEndCall(string s) => OnGameEnd?.Invoke(s);
        public void Initiate(List<FSPlayer> players, FSUIManager uiManager)
        {
            _playerList = players;
            _uiManager = uiManager;
            _gameOverManager = new FSGameOverManager(_playerList);
            _gameOverManager.OnGameEnd += GameEndCall;
        }

        private void OnDestroy()
        {
            _gameOverManager.OnGameEnd -= GameEndCall;
            _gameOverManager.DestroyThenPlay(_playerList);
        }
        private void Start()
        {
            _inGame = true;
            StartGame(_currentStage);
        }
        private void Update()
        {
            if (_gameOverManager.UpdateFinalCountdown())
                EndGame();
        }

        private void StartGame(int index)                                            // Receives index of current stage and checks if stage is available. Start game if it is or ends game if it isn't // 현재 스테이지의 인덱스를 받고 값에 따라 게임을 시작 혹은 끝낸다
        {
            if (IsStageAvailable(index) & _inGame)
            {
                GetGameVirables(index);
                StartCoroutine(StartGameCoroutine());
            }
            else
                EndGame();
        }
        public void EndGame()                                                        // Method to run if game has ended // 게임이 끝날 때 실행되는 메서드
        {
            _inGame = false;
            StartCoroutine(EndGameCoroutine());
        }

        private IEnumerator EndGameCoroutine()
        {
            yield return new WaitForSeconds(3.0f);
            _gameOverManager.GameOver(_playerList);
        }
        private bool IsStageAvailable(int stageIndex)                                // Method to check if stage is available using index of current stage. Returns true if stage with given index exists in stage list. return false otherwise
        {                                                                            // 길어서 여기에 씀: 현재 스테이지의 인덱스를 통해 스테이지 플레이가 가능한지 확인하는 메서드. 스테이지 리스트에 주어진 인덱스의 스테이지가 존재할 경우 true, 아니면 false를 반환한다!
            return stageIndex < stageList.FSStageList.Length;
        }
        private void GetGameVirables(int stageIndex)                                 // Method to reset variable from stage data with stage index // 스테이지 인덱스를 통해 스테이지 데이터릐 변수들을 초기화하는 메서드
        {
            FSStageSO currentStage = stageList.FSStageList[stageIndex];
            _colorDelay = currentStage.ColorDelayTime;
            _colorCount = currentStage.ColorCount;
            _currentStage = currentStage.StageIndex;
            _plateDisableDuration = currentStage.PlateDisappearDuration;
            _countDownTime = currentStage.CountDownTime;
        }
        private IEnumerator StartGameCoroutine()                                     // Coroutine to manage game stages 게임 스테이지들을 관리하는 코루틴
        {
            yield return new WaitForSeconds(3f);
            OnCameraViewChange?.Invoke(FSCameraView.SCREEN);
            yield return new WaitForSeconds(3f);
            for (int i = 0; i < _colorCount; i++)
            {
                OnPlateQueue?.Invoke();
                yield return new WaitForSeconds(_colorDelay);
                OnScreenReset?.Invoke();
                yield return new WaitForSeconds(_colorDelay);
            }
            OnCameraViewChange?.Invoke(FSCameraView.GAME);
            yield return new WaitForSeconds(5f);

            for (int i = _colorCount; i > 0; i--)
            {
                if (!_inGame)
                    break;
                for (int j = _countDownTime; j > 0; j--)
                {
                    OnCountDown?.Invoke(j);
                    yield return new WaitForSeconds(1f);
                }
                OnPlateDequeue.Invoke(_plateDisableDuration);
                yield return new WaitForSeconds(_plateDisableDuration);
            }
            StartGame(_currentStage);
        }
    }
}
