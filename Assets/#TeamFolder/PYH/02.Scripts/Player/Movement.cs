using UnityEngine;
using UnityEngine.InputSystem;

namespace PYH.Player
{
    public class Movement : PlayerModuleBase
    {
        private Vector3 _calcDir;

        [SerializeField] private float _speed;
        [SerializeField] private float _gravity;
        private Rigidbody _rigid;
        private bool _canMove = true;
        private bool _init;
        public override void Initialize(Player player)
        {
            if (_init) return;

            _init = true;
            _rigid = player.Rigid;
        }

        private void FixedUpdate()
        {
            _rigid.linearVelocity = _calcDir;
        }

        public void OnMove(InputValue value) // For Test Movement
        {
            if (!_init || !_canMove) return;

            Vector2 calcDir = value.Get<Vector2>() * _speed;

            _calcDir = new Vector3(calcDir.x,
                _rigid.linearVelocity.y,
                calcDir.y);
        }
    }
}
