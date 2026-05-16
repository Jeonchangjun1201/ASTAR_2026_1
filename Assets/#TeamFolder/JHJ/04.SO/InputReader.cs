using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static JHJControls;
namespace JHJ.Test.TestPlayer
{
    [CreateAssetMenu(fileName = "InputReader", menuName = "InputReSO/InputReader")]
    public class InputReader : ScriptableObject, IPlayerActions
    {
        private JHJControls inputcontrols;
        public event Action<Vector2> P1OnMove;
        public event Action<Vector2> P2OnMove;
        public event Action<Vector2> P3OnMove;
        public event Action<Vector2> P4OnMove;
        
        public event Action P1OnJump;
        public event Action P2OnJump;
        public Vector3 moveDir1 { get; private set; }
        public Vector2 moveDir2 { get; private set; }

        private void OnEnable()
        {
            if (inputcontrols == null)
            {
                inputcontrols = new JHJControls();
                inputcontrols.Player.SetCallbacks(this);
            }
            inputcontrols.Player.Enable();
        }

        public void OnJump1(InputAction.CallbackContext context)
        {
            if (context.performed) P1OnJump?.Invoke();
        }
        private void OnDisable()
        {
            inputcontrols.Player.Disable();
        }
        public void OnMovement1(InputAction.CallbackContext context)
        {
            moveDir1 = context.ReadValue<Vector2>();

            if (context.performed || context.canceled)
                P1OnMove?.Invoke(context.ReadValue<Vector2>());

        }

        public void OnMovement2(InputAction.CallbackContext context)
        {
            moveDir2 = context.ReadValue<Vector2>();
            if (context.performed || context.canceled)
                P2OnMove?.Invoke(context.ReadValue<Vector2>());
        }

        public void OnMovement3(InputAction.CallbackContext context)
        {

        }

        public void OnMovement4(InputAction.CallbackContext context)
        {

        }

    }
}