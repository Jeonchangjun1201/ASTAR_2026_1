using System;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public class TimerService : MonoBehaviour, ITimerService
    {
        public float Remaining { get; private set; }

        public event Action<float> OnTimerUpdated;
        public event Action        OnTimerExpired;

        private bool _running;

        public void StartTimer(float duration)
        {
            Remaining = duration;
            _running  = true;
        }

        public void StopTimer()
        {
            _running = false;
        }

        private void Update()
        {
            if (!_running) return;

            Remaining -= Time.deltaTime;
            OnTimerUpdated?.Invoke(Remaining);

            if (Remaining > 0f) return;

            Remaining = 0f;
            _running  = false;
            OnTimerExpired?.Invoke();
        }
    }
}
