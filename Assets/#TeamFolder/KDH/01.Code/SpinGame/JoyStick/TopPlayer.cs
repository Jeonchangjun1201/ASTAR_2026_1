using UnityEngine;

namespace KDH
{
    public class TopPlayer : MonoBehaviour
    {
        [Header("조이스틱")]
        public FixedJoystick joystick;

        [Header("이동")]
        public float moveSpeed = 5f;

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void Move()
        {
            Vector3 dir = new Vector3(
                joystick.Horizontal,
                0f,
                joystick.Vertical
            );

            rb.MovePosition(
                rb.position + dir * moveSpeed * Time.fixedDeltaTime
            );
        }
    }
}