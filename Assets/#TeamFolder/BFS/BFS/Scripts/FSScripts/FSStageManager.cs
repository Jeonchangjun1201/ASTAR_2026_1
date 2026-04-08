using System;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
namespace GDH
{
    public class FSStageManager : MonoBehaviour
    {
        [SerializeField] private FSStageListSO stageList;
        public event Action<FSCameraView> OnCameraViewChange;
        public event Action OnPlateQueue;
        public event Action OnScreenReset;
        public event Action<float> OnPlateDequeue;

        private float _colorDelay;
        private int _colorCount;
        private int _currentStage = 0;
        private float _plateDisableDuration;
        private int _countDownTime;
        private bool _inGame;


        private void Start()
        {
            StartGame(_currentStage);
        }

        private void StartGame(int index)
        {
            if (IsStageAvailable(index))
            {
                GetGameVirables(index);
                StartCoroutine(StartGameCoroutine());
            }
            else
                EndGame();
        }
        private void EndGame()
        {
            Debug.Log("FINISHED!");
        }
        private bool IsStageAvailable(int stageIndex)
        {
            return stageIndex < stageList.FSStageList.Length ? true : false;
        }
        private void GetGameVirables(int stageIndex)
        {
            FSStageSO currentStage = stageList.FSStageList[stageIndex];
            _colorDelay = currentStage.ColorDelayTime;
            _colorCount = currentStage.ColorCount;
            _currentStage = currentStage.StageIndex;
            _plateDisableDuration = currentStage.PlateDisappearDuration;
            _countDownTime = currentStage.CountDownTime;
        }
        private IEnumerator StartGameCoroutine()
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
