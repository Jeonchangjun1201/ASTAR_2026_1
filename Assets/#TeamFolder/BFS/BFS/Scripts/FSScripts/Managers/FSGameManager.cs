using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BFS
{
    public class FSGameManager : MonoBehaviour                                // Game manager script for Four Side game
    {
        [SerializeField] private FSPlateManager plateManager;
        [SerializeField] private FSCameraManager cameraManager;
        [SerializeField] private FSStageManager stageManager;
        private List<FSPlayer> _playerList = new List<FSPlayer>();
        private IFSScreen monitorScreen;
        private int _aliveCount;
        private bool _finalCountActivated = false;
        private void Awake()
        {
            FSPlayer[] playrListTemp = GetComponentsInChildren<FSPlayer>();
            foreach(FSPlayer player in playrListTemp)
            {
                _playerList.Add(player);
                player.OnOut += CountOuts;
                _aliveCount++;
            }
            cameraManager.FocusToGame();                                  
            monitorScreen = GetComponentInChildren<IFSScreen>();
            plateManager.OnPlateAdded += ManageScreenColor;                   // Subscribes, monitor screen color is changed everytime plate is added to queue

            stageManager.OnCameraViewChange += ChangeCameraView;              // Subscribes, now camera view will change depending on parameter sent from stage manager
            stageManager.OnPlateQueue += QueuePlate;                          // Subscribes, stage manager can alert plate manager to que plates now
            stageManager.OnScreenReset += ResetScreen;                        // Subscribes, stage manager can reset monitor screen to default
            stageManager.OnPlateDequeue += DeQueuePlate;                      // Subscribes, now stage manager can remove plates
        }

        private void Start()
        {

        }
        private void Update()
        {
            if (_aliveCount <= 1 & !_finalCountActivated)
            {
                StartCoroutine(FinalGameCountdownCoroutine());
                _finalCountActivated = true;
            }
        }

        private IEnumerator FinalGameCountdownCoroutine()
        {
            yield return new WaitForSeconds(3.0f);
            if( _aliveCount == 1 )
            {
                FinishGame();
                FSPlayer Lastplayer = null;
                foreach(FSPlayer player in _playerList)
                {
                    if (player.IsOut == false)
                        Lastplayer = player;
                }
                Debug.Log($"{Lastplayer.GetComponentInParent<PlayerBFS>().gameObject.name} WON!!");
            }
            else
            {
                FinishGame();
                Debug.Log("NO SURVIVORS! :(");
            }
        }

        private void OnDestroy()                                              // Unsub
        {
            plateManager.OnPlateAdded -= ManageScreenColor;

            stageManager.OnCameraViewChange += ChangeCameraView; 
            stageManager.OnPlateQueue += QueuePlate;   
            stageManager.OnScreenReset += ResetScreen;
            stageManager.OnPlateDequeue += DeQueuePlate;

            foreach(FSPlayer player in _playerList)
            {
                player.OnOut -= CountOuts;
            }
        }
        public void FinishGame()
        {
            stageManager.EndGame();
        }
        private void ChangeCameraView(FSCameraView cameraView)                // Method to change camera view, receiving FSCameraView enum that decides which camera to view    
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
                    throw new System.ArgumentException("INVALID TYPE");       // Exception
            }
        }
        private void ManageScreenColor(PlateColor plate)                      // Method to change monitor screen color
        {
            Color color = new Color();
            switch (plate)
            {
                case PlateColor.RED:
                    color = Color.red;
                    break;
                case PlateColor.GREEN:
                    color = Color.green;
                    break;
                case PlateColor.BLUE:
                    color = Color.blue;
                    break;
                case PlateColor.YELLOW:
                    color = Color.yellow;
                    break;
                default:
                    throw new System.ArgumentException("INVALID TYPE");
            }
            monitorScreen.ChangeScreenColor(color);
        }
        private void QueuePlate() => plateManager.EnqueuePlate();
        private void ResetScreen() => monitorScreen.ResetScreenColor();
        private void DeQueuePlate(float duration) => plateManager.DequeuePlate(duration);
        private void CountOuts() => _aliveCount--;
    }
}
