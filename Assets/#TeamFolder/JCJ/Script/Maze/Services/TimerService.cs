using System;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 라운드의 남은 시간을 갱신하고 만료 이벤트를 발생시키는 타이머 서비스.
    /// </summary>
    public class TimerService : MonoBehaviour, ITimerService
    {
        public float Remaining { get; private set; }

        public event Action<float> OnTimerUpdated;
        public event Action        OnTimerExpired;

        private bool _running;

        public void StartTimer(float duration)
        {
            Remaining = Mathf.Max(0f, duration);
            _running  = Remaining > 0f;
            OnTimerUpdated?.Invoke(Remaining);
        }

        public void StopTimer()
        {
            _running = false;
        }

        public void ResetTimer()
        {
            _running  = false;
            Remaining = 0f;
            OnTimerUpdated?.Invoke(Remaining);
        }

        public void AddTime(float seconds)
        {
            // 첫 골인 보너스처럼 남은 시간을 줄이는 기능도 같은 진입점으로 처리한다.
            if (!_running) return;
            Remaining = Mathf.Max(0f, Remaining + seconds);
            OnTimerUpdated?.Invoke(Remaining);
        }

        private void Update()
        {
            if (!_running) return;

            // 매 프레임 남은 시간을 알리면 HUD가 별도 폴링 없이 표시를 갱신할 수 있다.
            Remaining -= Time.deltaTime;
            OnTimerUpdated?.Invoke(Remaining);

            if (Remaining > 0f) return;

            Remaining = 0f;
            _running  = false;
            OnTimerExpired?.Invoke();
        }
    }
}
