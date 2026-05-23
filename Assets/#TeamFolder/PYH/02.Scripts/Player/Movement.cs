using UnityEngine;
using UnityEngine.InputSystem;

namespace _TeamFolder.PYH._02.Scripts.Player
{
    public class Movement : PlayerModuleBase
    {
        private Rigidbody rb;

        private Vector3 _calcDir;
        [SerializeField] private float speed;
        private readonly bool canMove = true;
        private bool _init;
        
        private Vector2 _moveInput;
        
        public override void Initialize(Player player)
        {
            if (_init) return;

            _init = true;
            rb = player.Rigid;
        }

        private void FixedUpdate()
        {
            if (!_init || !canMove) return;

            Vector2 input = Vector2.ClampMagnitude(_moveInput, 1f);

            Vector3 velocity = rb.linearVelocity;

            velocity.x = input.x * speed;
            velocity.z = input.y * speed;

            rb.linearVelocity = velocity;
        }
        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }
    }
}
