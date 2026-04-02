using UnityEngine;
using UnityEngine.InputSystem;

namespace PYH.Player
{
    public class Movement : MonoBehaviour
    {
        private Vector3 _calcDir;

        [SerializeField] private float _speed;
        private Rigidbody _rigid;
        private bool _canMove = true;
        private bool _init;
        public void Initialize(Rigidbody rigid)
        {
            if (_init) return;

            _init = true;
            _rigid = rigid;
        }

        private void FixedUpdate()
        {
            _rigid.AddForce(_calcDir);
        }

        public void OnMove(InputValue value) // For Test Movement
        {
            if (!_init || !_canMove) return;

            Vector2 calcDir = value.Get<Vector2>() * _speed;

            _calcDir = new Vector3(calcDir.x,
                0,
                calcDir.y);
        }
    }
}
