using System;
using System.Collections;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 라운드 시작 전 숫자 카운트다운을 진행하고 마지막에 GO 이벤트를 보낸다.
    /// </summary>
    public class CountdownService : MonoBehaviour, ICountdownService
    {
        public event Action<int> OnTick;
        public event Action OnGo;

        private Coroutine _routine;

        public void Begin(int seconds)
        {
            // 새 카운트다운을 시작하기 전 기존 코루틴을 정리해 중복 Tick을 막는다.
            Cancel();
            _routine = StartCoroutine(RunCountdown(Mathf.Max(1, seconds)));
        }

        public void Cancel()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private IEnumerator RunCountdown(int seconds)
        {
            for (int i = seconds; i >= 1; i--)
            {
                OnTick?.Invoke(i);
                yield return new WaitForSeconds(1f);
            }
            OnGo?.Invoke();
            _routine = null;
        }
    }
}
