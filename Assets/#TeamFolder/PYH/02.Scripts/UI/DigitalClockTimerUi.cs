using System;
using System.Collections;
using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class DigitalClockTimerUi : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        private int _selectedTime;
        private int _curTime;

        public event Action OnTimeEndedEvent;
        
        private void Awake()
        {
            AStarEventBus.Subscribe<DigitalClockUiTimeSetEvent>(SetTime);
            AStarEventBus.Subscribe<DigitalClockUiStartEvent>(StartTimer);
        }
        
        private void OnDestroy()
        {
            AStarEventBus.Unsubscribe<DigitalClockUiTimeSetEvent>(SetTime);
            AStarEventBus.Unsubscribe<DigitalClockUiStartEvent>(StartTimer);
        }
        
        private void SetTime(DigitalClockUiTimeSetEvent @event)
        {
            _selectedTime = @event.SEC;
            
            int minutes = _selectedTime / 60;
            int seconds = _selectedTime % 60;

            string timeText = $"{minutes:00}:{seconds:00}";
            
            label.text = timeText;
        }

        private void StartTimer(DigitalClockUiStartEvent @event)
        {
            StopAllCoroutines();;
            StartCoroutine(TimerCoroutine());
        }

        private IEnumerator TimerCoroutine()
        {
            _curTime = _selectedTime;

            while (_curTime >= 0)
            {
                int minutes = _curTime / 60;
                int seconds = _curTime % 60;

                string timeText = $"{minutes:00}:{seconds:00}";

                label.text = timeText;

                yield return new WaitForSeconds(1);
                
                _curTime--;
            }
            
            OnTimeEndedEvent?.Invoke();
        }
    }
}
