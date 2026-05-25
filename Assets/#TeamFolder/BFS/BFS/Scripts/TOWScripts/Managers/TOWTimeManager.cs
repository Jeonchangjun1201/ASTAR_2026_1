using System;
using UnityEngine;
namespace BFS
{
    public class TOWTimeManager
    {
        private TOWUIManager _uiManager;
        private float _elapsedTime;
        private float _endTime;
        public event Action OnTimerEnd;
        private bool _isRunning = false;

        public TOWTimeManager(TOWUIManager uiManager)
        {
            _uiManager = uiManager;
        }
        public void StartTimer(float endTime)
        {
            _elapsedTime = 0;
            _endTime = endTime;
            _isRunning = true;
        }

        public void UpdateTimer()
        {
            if (!_isRunning)
                return;
            _elapsedTime += Time.deltaTime;
            int timer = (int)_endTime - (int)_elapsedTime;
            if (_elapsedTime > _endTime)
            {
                EndTimer();
            }
        }
        public void EndTimer()
        {
            OnTimerEnd?.Invoke();
            _isRunning = false;
        }
    }

}
