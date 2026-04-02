using System; // Action을 사용하기 위해 필요
using UnityEngine;
using UnityEngine.InputSystem;
using static JHJControls;
namespace JHJ.Test.TestPlayer
{
    [CreateAssetMenu(fileName = "InputReader", menuName = "InputReSO/InputReader")]
    public class InputReader : ScriptableObject, IPlayerActions
    {
        private JHJControls inputcontrols;
        public event Action<Vector2> OnMoveEvent;
        public Vector3 moveDir { get; private set; }

        private void OnEnable()
        {
            if (inputcontrols == null)
            {
                inputcontrols = new JHJControls();
                inputcontrols.Player.SetCallbacks(this);
            }
            inputcontrols.Player.Enable();
        }

        private void OnDisable()
        {
            inputcontrols.Player.Disable();
        }

        public void OnMoveMent(InputAction.CallbackContext context)
        {
            moveDir = context.ReadValue<Vector2>();

            if (context.performed || context.canceled)
                OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
        }
    }
}