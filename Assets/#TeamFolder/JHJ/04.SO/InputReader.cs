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

        public event Action<Vector2> P1OnLook;  // 마우스 델타
        public event Action<Vector2> P2OnLook;
        public event Action<float> P1OnZoom;  // 스크롤 .y
        public event Action<float> P2OnZoom;




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

        // OnLook1 - 마우스 델타값을 P1OnLook으로 전달
        public void OnLook1(InputAction.CallbackContext context)
        {
            Debug.Log($"OnLook1 호출됨: {context.ReadValue<Vector2>()}");
            if (context.performed || context.canceled)
                P1OnLook?.Invoke(context.ReadValue<Vector2>());
        }

        // OnZoom1 - 스크롤 Vector2의 .y만 float으로 전달
        public void OnZoom1(InputAction.CallbackContext context)
        {
            if (context.performed || context.canceled)
                P1OnZoom?.Invoke(context.ReadValue<Vector2>().y);
        }

        public void OnMovement3(InputAction.CallbackContext context)
        {
            
        }

        public void OnMovement4(InputAction.CallbackContext context)
        {
            
        }
    }
}