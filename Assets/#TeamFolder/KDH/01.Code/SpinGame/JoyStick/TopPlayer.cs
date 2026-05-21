using UnityEngine;

namespace KDH
{
    public class TopPlayer : MonoBehaviour
    {
        [Header("조이스틱")]
        [SerializeField] private FixedJoystick joystick;

        [Header("이동")]
        [SerializeField] private float moveSpeed = 5f;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (joystick == null) return;
            if (Mathf.Abs(joystick.Horizontal) < 0.01f && 
                Mathf.Abs(joystick.Vertical) < 0.01f) return;

            Vector3 dir = new Vector3(joystick.Horizontal, 0f, joystick.Vertical);
            Vector3 newPos = _rb.position + dir * moveSpeed * Time.fixedDeltaTime;
            newPos.y = _rb.position.y; // Y축 고정
            _rb.MovePosition(newPos);
        }
    }
}