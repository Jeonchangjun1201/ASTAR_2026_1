using UnityEngine;
using UnityEngine.InputSystem;

namespace KSY.Clients
{
    public class PlayerInputReader : InputReaderBase, KSY_InputActions.IPlayerActions
    {
        private InputActionMap inputActionMap = null;
        public override InputActionMap GetInputActionMap() => inputActionMap;

        public Vector2 MovementInput { get; private set; }

        public override void Initialize(KSY_InputActions inputActions)
        {
            base.Initialize(inputActions);

            KSY_InputActions.PlayerActions playerActions = inputActions.Player;
            playerActions.SetCallbacks(this);
            inputActionMap = playerActions.Get();
        }

        void KSY_InputActions.IPlayerActions.OnMove(InputAction.CallbackContext context)
        {
            if (context.canceled)
            {
                MovementInput = Vector2.zero;
                return;
            }

            MovementInput = context.ReadValue<Vector2>().normalized;
        }
    }
}



