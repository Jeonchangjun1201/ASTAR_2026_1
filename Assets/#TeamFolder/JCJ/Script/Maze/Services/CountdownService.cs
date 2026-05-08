using System;
using System.Collections;
using UnityEngine;

// 라운드 시작 카운트다운을 관리하는 서비스.

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

        // 카운트다운 시작 요청 진입점이다.
        // 서버 구조에서는 GO 시각을 포함한 시작 메시지를 받은 뒤 이 메서드로 화면만 재생하면 된다.
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

        // 숫자 Tick과 마지막 GO 이벤트를 만드는 실제 코루틴이다.
        // 서버 시간을 기준으로 맞출 경우에도 로컬 연출 재생 자체는 이 흐름으로 유지하기 쉽다.
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
