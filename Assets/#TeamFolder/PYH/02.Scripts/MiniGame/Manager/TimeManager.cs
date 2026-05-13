using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PYH.Manager
{
    public class TimeManager : MonoBehaviour
    {
        private bool _init;
        public UnityEvent OnTickInitEvent;
        public UnityEvent OnTickStartEvent;
        public UnityEvent<int> OnTickEvent;
        public UnityEvent OnTickEndEvent;
        
        [SerializeField] private int _maxTime = 50;
        [SerializeField] private int _currentTime = 0;

        public void Initialize()
        {
            if (_init) return;

            _init = true;
            OnTickInitEvent?.Invoke();
            
            StartCoroutine(Timer());
        }

        public void StopTimer()
        {
            StopAllCoroutines();
            _init = false;
        }
        
        private IEnumerator Timer()
        {
            OnTickStartEvent?.Invoke();
            _currentTime = _maxTime;

            while (_currentTime > 0)
            {
                _currentTime -= 1;
                OnTickEvent?.Invoke(_currentTime);
                yield return new WaitForSeconds(1);
            }

            OnTickEndEvent?.Invoke();
        }
    }
}
