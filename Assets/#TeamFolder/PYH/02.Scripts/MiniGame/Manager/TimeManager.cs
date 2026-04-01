using System;
using System.Collections;
using UnityEngine;

namespace PYH.Manager
{
    public class TimeManager : MonoBehaviour
    {
        private bool _init;

        [SerializeField] private int _maxTime = 50;
        [SerializeField] private int _currentTime = 0;

        public event Action OnTimerEndEvent;

        public void Initialize()
        {
            if (_init) return;

            _init = true;

            StartCoroutine(Timer());
        }

        private IEnumerator Timer()
        {
            _currentTime = _maxTime;

            while (_currentTime > 0)
            {
                _currentTime -= 1;
                yield return new WaitForSeconds(1);
            }

            OnTimerEndEvent?.Invoke();
        }
    }
}
