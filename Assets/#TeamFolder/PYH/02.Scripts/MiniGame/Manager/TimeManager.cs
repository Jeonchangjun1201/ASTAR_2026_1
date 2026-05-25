using System.Collections;
using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;
using UnityEngine.Events;

namespace _TeamFolder.PYH._02.Scripts.MiniGame.Manager
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
            
            AStarEventBus.Publish(new DigitalClockUiTimeSetEvent(_maxTime));
            
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
                AStarEventBus.Publish(new DigitalClockUiTimeSetEvent(_currentTime));

                if (_currentTime > 5)
                {
                    SoundManager.Instance.PlaySound("General-Countdown-S");
                }
                else
                {
                    SoundManager.Instance.PlaySound("General-Countdown-S-Stress");
                }
                yield return new WaitForSeconds(1);
            }

            OnTickEndEvent?.Invoke();
        }
    }
}
