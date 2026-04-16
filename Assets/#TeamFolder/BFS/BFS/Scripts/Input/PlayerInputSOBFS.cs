using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BFS
{

    [CreateAssetMenu(fileName = "PlayerInputSOBFS", menuName = "BFS_SO/PlayerInputSOBFS")]
    public class PlayerInputSOBFS : ScriptableObject, BFSTempPlayerControls.IPlayerActions       // My own PlayerInputSO (Does it need more explanation?)
    {
        public event Action<Vector2> OnMovementKeyPressed;                                       // Action that invokes whenever movement key is pressed
        public event Action OnJumpKeyPressed;                                                    // Action that invokes whenever jump key is pressed

        BFSTempPlayerControls _controls;

        private void OnEnable()                                                                  // Preparing for Controls
        {
            if (_controls == null)
            {
                _controls = new BFSTempPlayerControls();
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
            Vector2 movementInput = context.ReadValue<Vector2>();
            OnMovementKeyPressed?.Invoke(movementInput);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnJumpKeyPressed?.Invoke();
        }
    }
}
