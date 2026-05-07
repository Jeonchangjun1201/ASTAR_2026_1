using System;
using UnityEngine;

// 라운드 제한 시간과 남은 시간을 관리하는 서비스.

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

        // 라운드 타이머를 시작하고 초기 남은 시간을 브로드캐스트한다.
        // 서버 기준 남은 시간을 쓰게 되면 시작 시각 동기화나 스냅샷 반영이 이 메서드 책임과 맞닿는다.
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

        // 남은 시간을 줄이고 HUD용 이벤트를 계속 발행한다.
        // 멀티에서 서버 시간이 따로 있다면 이 로컬 감소 루프 대신 서버값 보간으로 대체될 수 있다.
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
