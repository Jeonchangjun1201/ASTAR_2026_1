using UnityEngine;

namespace KDH
{
    public class TopController : MonoBehaviour
    {
        [Header("조이스틱")]
        public FixedJoystick joystick;

        [Header("이동")]
        public float moveSpeed = 5f;

        [Header("회전")]
        public float spinSpeed = 1000f;

        [Header("시간 흔들림")]
        public float wobbleSpeed = 6f;
        public float wobbleIncreaseSpeed = 0.5f;
        public float maxWobbleAngle = 15f;

        [Header("충돌 흔들림")]
        public float hitWobbleAmount = 4f;
        public float hitRecoverSpeed = 2f;

        private Rigidbody rb;

        private float currentWobble = 0f;
        private float hitWobble = 0f;
        private float currentYRotation = 0f;
        private float timeValue = 0f;

        private Quaternion startRotation;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            startRotation = transform.rotation;
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void Update()
        {
            SpinAndWobble();
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

        private void SpinAndWobble()
        {
            timeValue += Time.deltaTime;

            currentWobble += wobbleIncreaseSpeed * Time.deltaTime;
            currentWobble = Mathf.Clamp(
                currentWobble,
                0f,
                maxWobbleAngle
            );

            hitWobble = Mathf.Lerp(
                hitWobble,
                0f,
                Time.deltaTime * hitRecoverSpeed
            );

            float totalWobble = currentWobble + hitWobble;

            currentYRotation += spinSpeed * Time.deltaTime;

            float xTilt = Mathf.Sin(timeValue * wobbleSpeed) * totalWobble;
            float zTilt = Mathf.Cos(timeValue * wobbleSpeed) * totalWobble;

            Quaternion wobbleRotation = Quaternion.Euler(
                xTilt,
                currentYRotation,
                zTilt
            );

            transform.rotation = startRotation * wobbleRotation;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Top"))
            {
                hitWobble += hitWobbleAmount;
            }
        }
    }
}