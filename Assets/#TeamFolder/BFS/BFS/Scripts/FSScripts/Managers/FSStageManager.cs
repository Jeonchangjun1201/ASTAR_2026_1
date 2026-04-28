using System;
using System.Collections;
using UnityEngine;
namespace BFS
{
    public class FSStageManager : MonoBehaviour                                      // Stage manager for Four Sides game
    {
        [SerializeField] private FSStageListSO stageList;                            // StageList
        public event Action<FSCameraView> OnCameraViewChange;                        // Action to change camera view
        public event Action OnPlateQueue;                                            // Action to add plates to queue(and change color screen with game manager)
        public event Action OnScreenReset;                                           // Action to reset monitor screen
        public event Action<float> OnPlateDequeue;                                   // Action to remove/deactivate plates

        private float _colorDelay;
        private int _colorCount;
        private int _currentStage = 0;
        private float _plateDisableDuration;
        private int _countDownTime;
        private bool _inGame;


        private void Start()
        {
            _inGame = true;
            StartGame(_currentStage);
        }

        private void StartGame(int index)                                            // Receives index of current stage and checks if sstage is available. Start game if it is or ends game if it isn't
        {
            if (IsStageAvailable(index) & _inGame)
            {
                GetGameVirables(index);
                StartCoroutine(StartGameCoroutine());
            }
            else
                EndGame();
        }
        public void EndGame()                                                        // Method to run if game has ended
        {
            _inGame = false;
            Debug.Log("FINISHED!");                                                  // TEMPORARY; for debugging
        }
        private bool IsStageAvailable(int stageIndex)                                // Method to check if stage is available using index of current stage. Returns true if stage with given index exists in stage list. return false otherwise
        {
            return stageIndex < stageList.FSStageList.Length;
        }
        private void GetGameVirables(int stageIndex)                                 // Method to reset variable from stage data with stage index
        {
            FSStageSO currentStage = stageList.FSStageList[stageIndex];
            _colorDelay = currentStage.ColorDelayTime;
            _colorCount = currentStage.ColorCount;
            _currentStage = currentStage.StageIndex;
            _plateDisableDuration = currentStage.PlateDisappearDuration;
            _countDownTime = currentStage.CountDownTime;
        }
        private IEnumerator StartGameCoroutine()                                     // Coroutine to manage game stages
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
                    Debug.Log(j);
                    yield return new WaitForSeconds(1f);
                }
                OnPlateDequeue.Invoke(_plateDisableDuration);
                yield return new WaitForSeconds(_plateDisableDuration);
            }
            StartGame(_currentStage);
        }
    }
}
