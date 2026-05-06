using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BFS
{

    [CreateAssetMenu(fileName = "PlayerInputSOBFS", menuName = "BFS_SO/PlayerInputSOBFS")]
    public class PlayerInputSOBFS : ScriptableObject, BFSTempPlayerControls.IPlayerActions       // My own PlayerInputSO (Does it need more explanation?) // 내가 쓰기 위해 만든 플레이어 인풋SO, 나중에 변경 가능
    {
        public event Action<Vector2> OnMovementKeyPressed;                                       // Action that invokes whenever movement key is pressed // 이동 키 눌렀을 때 실행하는 액션
        public event Action OnJumpKeyPressed;                                                    // Action that invokes whenever jump key is pressed // 점프 키 눌렀을 때 실행하는 액션

        BFSTempPlayerControls _controls;

        private void OnEnable()                                                                  // Preparing for Controls // 컨트롤즈 준비
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
