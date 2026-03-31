using UnityEngine;

namespace PYH.Player
{
    public class Movement : MonoBehaviour
    {
        private Rigidbody _rigid;
        private bool _canMove = true;
        private bool _init;
        public void Initialize(Rigidbody rigid)
        {
            if (_init) return;

            _init = true;
            _rigid = rigid;
        }

        public void Update() // For Test Movement
        {
            if (!_init) return;
            if (!_canMove) return;
        }
    }
}
