using System;
using UnityEngine;
namespace BFS
{
    public class TOWTimeManager
    {
        private float _elapsedTime;
        private float _endTime;
        public event Action OnTimerEnd;
        private bool _isRunning = false;

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
            if (_elapsedTime > _endTime)
            {
                OnTimerEnd?.Invoke();
                _isRunning = false;
            }
        }
    }

}
