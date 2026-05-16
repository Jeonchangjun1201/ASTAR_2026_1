using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace BFS
{
    public class FSGameManager : MonoBehaviour                                // Game manager script for Four Side game // 게임 매니저!!
    {
        [SerializeField] private FSPlateManager plateManager;
        [SerializeField] private FSCameraManager cameraManager;
        [SerializeField] private FSStageManager stageManager;
        private List<FSPlayer> _playerList = new List<FSPlayer>();
        private FSScreenManager _screenManager;
        private FSUIManager _uiManager;
        private void Awake()
        {
            FSPlayer[] playrListTemp = GetComponentsInChildren<FSPlayer>();
            foreach (FSPlayer player in playrListTemp)
            {
                _playerList.Add(player);
            }
            cameraManager.FocusToGame();
            _screenManager = GetComponentInChildren<FSScreenManager>();
            _uiManager = GetComponentInChildren<FSUIManager>();
            plateManager.OnPlateAdded += PlateAddedToQue;                     // Subscribes, monitor screen color is changed everytime plate is added to queue // 구독함, 발판이 큐에 추가될 때마다 모니터 화면 변경됨

            stageManager.Initiate(_playerList, _uiManager);
            stageManager.OnCameraViewChange += ChangeCameraView;              // Subscribes, now camera view will change depending on parameter sent from stage manager // 구독함, 스테이지 매니저가 보내는 매개변수에 따라 카메라 시점 변경
            stageManager.OnPlateQueue += QueuePlate;                          // Subscribes, stage manager can alert plate manager to que plates now // 구독함, 이제 스테이지 매니저가 발판을 큐에 넣으라고 알려줄 수 있음
            stageManager.OnScreenReset += ResetScreen;                        // Subscribes, stage manager can reset monitor screen to default // 구독함, 스테이지 매니저가 모니터 화면을 기본상태로 변경 가능
            stageManager.OnPlateDequeue += DeQueuePlate;                      // Subscribes, now stage manager can remove plates // 구독, 스테이지 매니저가 발판들을 삭제할 수 있음
            stageManager.OnCountDown += TimeCountDown;                        // Subscribes, stage manager can now access to ui and use countdown // 구독, 스테이지 매니저가 UI를 통해 카운트다운을 실행함
            stageManager.OnGameEnd += SetEndText;                             // Subscribes, sets game over text ui // 구독, 게임 오버 UI 작동함
        }

        private void Start()
        {

        }

        private void OnDestroy()                                              // Unsub // 구독 해제
        {
            plateManager.OnPlateAdded -= PlateAddedToQue;

            stageManager.OnCameraViewChange -= ChangeCameraView;
            stageManager.OnPlateQueue -= QueuePlate;
            stageManager.OnScreenReset -= ResetScreen;
            stageManager.OnPlateDequeue -= DeQueuePlate;
            stageManager.OnCountDown -= TimeCountDown;
            stageManager.OnGameEnd -= SetEndText;
        }
        private void ChangeCameraView(FSCameraView cameraView)                // Method to change camera view, receiving FSCameraView enum that decides which camera to view // 카메라 시점을 변경하는 메서드, 매개변수로 카메라 시점 지정
        {
            switch (cameraView)
            {
                case FSCameraView.GAME:
                    cameraManager.FocusToGame();
                    break;
                case FSCameraView.SCREEN:
                    cameraManager.FocusToScreen();
                    break;
                default:
                    throw new System.ArgumentException("INVALID TYPE");       // Exception // 예외 처리
            }
        }
        private void PlateAddedToQue(PlateColor color)                      // Method to change monitor screen color // 모니터 화면 색을 변경하는 메서드
        {
            _screenManager.ChangeScreenColor(color);
            string s = GetTextColor(color);
            _uiManager.ChangeText(_uiManager.ColorText, s + $"- {color} -</color>", stageManager.ColorDelay);
        }
        private void QueuePlate() => plateManager.EnqueuePlate();
        private void ResetScreen() => _screenManager.ResetScreenColor();
        private void DeQueuePlate(float duration)
        {
            PlateColor targetColor = plateManager.plateQue.plateQueue.First<PlateColor>();
            string s = GetTextColor(targetColor);
            _uiManager.ChangeText(_uiManager.ColorText, s + $"! {targetColor} !</color>", stageManager.PlateDisableDuration);
            plateManager.DequeuePlate(duration);
        }
        private void TimeCountDown(int time)
        {
            _uiManager.ChangeText(_uiManager.CountDownText, time.ToString(), 1);
        }
        private string GetTextColor(PlateColor color)
        {
            string s;
            switch (color)
            {
                case PlateColor.RED:
                    s = "<color=red>";
                    break;
                case PlateColor.GREEN:
                    s = "<color=green>";
                    break;
                case PlateColor.BLUE:
                    s = "<color=blue>";
                    break;
                case PlateColor.YELLOW:
                    s = "<color=yellow>";
                    break;
                default:
                    throw new System.ArgumentException();
            }
            return s;
        }

        private void SetEndText(string s)
        {
            _uiManager.ChangeText(_uiManager.GameOverText, s, 0);
        }
    }
}
