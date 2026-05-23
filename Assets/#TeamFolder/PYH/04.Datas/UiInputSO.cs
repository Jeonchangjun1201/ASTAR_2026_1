using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _TeamFolder.PYH._04.Datas
{
    [CreateAssetMenu(fileName = "UiInputSO", menuName = "PYH/UiInputSO")]
    public class UiInputSO : ScriptableObject, Controls.IUIActions
    {
        private Controls _controls;

        public event Action OnSettingEvent, OnGuideEvent;
        
        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.UI.SetCallbacks(this);
            }
            _controls.UI.Enable();
        }
        private void OnDisable()
        {
            if(_controls != null)
                _controls.UI.Disable();
        }
        
        public void OnSetting(InputAction.CallbackContext context)
        {
            OnSettingEvent?.Invoke();
        }
        public void OnGuide(InputAction.CallbackContext context)
        {
            OnGuideEvent?.Invoke();
        }
    }
}
