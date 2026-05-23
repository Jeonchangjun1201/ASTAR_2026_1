using System;
using System.Collections;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public class CountdownService : MonoBehaviour, ICountdownService
    {
        public event Action<int> OnTick;
        public event Action OnGo;

        [SerializeField] private CountdownUI _countdownUI;

        private Coroutine _routine;

        private void Awake()
        {
            _countdownUI ??= GetComponent<CountdownUI>();
            _countdownUI ??= GetComponentInChildren<CountdownUI>(true);
            if (_countdownUI == null)
                _countdownUI = FindFirstObjectByType<CountdownUI>();
        }

        public void Begin(int seconds)
        {
            Cancel();
            if (_countdownUI != null)
            {
                _countdownUI.OnTick += HandleTick;
                _countdownUI.OnGo += HandleGo;
                _routine = StartCoroutine(RunWithUi(seconds));
                return;
            }
            _routine = StartCoroutine(RunCountdown(Mathf.Max(1, seconds)));
        }

        public void Cancel()
        {
            if (_countdownUI != null)
            {
                _countdownUI.OnTick -= HandleTick;
                _countdownUI.OnGo -= HandleGo;
                _countdownUI.Cancel();
            }
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private IEnumerator RunWithUi(int seconds)
        {
            yield return _countdownUI.PlayRoutine(seconds);
            _countdownUI.OnTick -= HandleTick;
            _countdownUI.OnGo -= HandleGo;
            _routine = null;
        }

        private IEnumerator RunCountdown(int seconds)
        {
            yield return JcjCountdownRunner.RunSeconds(
                seconds,
                i => OnTick?.Invoke(i),
                () => OnGo?.Invoke());
            _routine = null;
        }

        private void HandleTick(int value) => OnTick?.Invoke(value);
        private void HandleGo() => OnGo?.Invoke();
    }
}
