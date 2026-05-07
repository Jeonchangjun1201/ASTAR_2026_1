using UnityEngine;
using UnityEngine.InputSystem;

namespace PYH.Player
{
    public class Movement : PlayerModuleBase
    {
        private Vector3 _calcDir;

        [SerializeField] private float speed;
        [SerializeField] private float gravity;
        [SerializeField] private CharacterController characterController;
        private readonly bool canMove = true;
        private bool _init;
        
        private Vector2 _moveInput;
        
        public override void Initialize(Player player)
        {
            if (_init) return;

            _init = true;
            characterController = player.CharacterController;
        }

        private void FixedUpdate()
        {
            if (!_init || !canMove) return;

            Vector2 input = Vector2.ClampMagnitude(_moveInput, 1f);

            Vector3 moveDir = new Vector3(input.x, 0f, input.y);
            characterController.Move(moveDir * (speed * Time.deltaTime));
        }

        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }
    }
}
