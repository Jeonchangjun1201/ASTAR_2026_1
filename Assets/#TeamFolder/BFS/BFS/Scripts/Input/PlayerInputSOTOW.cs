using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace GDH
{
    [CreateAssetMenu(fileName = "PlayerInputSOTOW", menuName = "BFS_SO/PlayerInputSOTOW")]
    public class PlayerInputSOTOW : ScriptableObject, TOWControls.IPlayerActions
    {
        public event Action<Vector2> OnMovementInputPressed;

        private TOWControls _controls;

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new TOWControls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            if (_controls != null)
            {
                _controls.Player.Disable();
            }
        }
        public void OnMove(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Vector2 input = context.ReadValue<Vector2>();
                OnMovementInputPressed?.Invoke(input);
            }
        }
    }
}
