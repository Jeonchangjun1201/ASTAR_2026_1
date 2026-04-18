using UnityEngine;

namespace KDH
{
    public class TopSpin : MonoBehaviour
    {
        [Header("회전")]
        public float startSpinSpeed = 1000f;
        public float slowDownSpeed = 20f;

        [Header("흔들림")]
        public float wobbleSpeed = 4f;
        public float maxWobbleAngle = 25f;

        [Header("넘어짐")]
        public float fallSpinThreshold = 80f;

        private float currentSpinSpeed;
        private float currentYRotation = 0f;
        private float timeValue = 0f;

        private bool isFallen = false;
        private Quaternion startRotation;

        private void Awake()
        {
            currentSpinSpeed = startSpinSpeed;
            startRotation = transform.rotation;
        }

        private void Update()
        {
            if (isFallen) return;

            Spin();
        }

        private void Spin()
        {
            timeValue += Time.deltaTime;

            currentSpinSpeed -= slowDownSpeed * Time.deltaTime;
            currentSpinSpeed = Mathf.Max(currentSpinSpeed, 0f);

            float speedRatio = currentSpinSpeed / startSpinSpeed;


            float wobbleAmount =
                (1f - speedRatio * speedRatio) *
                maxWobbleAngle;

            currentYRotation +=
                currentSpinSpeed * Time.deltaTime;

            float xTilt =
                Mathf.Sin(timeValue * wobbleSpeed) *
                wobbleAmount;

            float zTilt =
                Mathf.Cos(timeValue * wobbleSpeed) *
                wobbleAmount;

            transform.rotation =
                startRotation *
                Quaternion.Euler(
                    xTilt,
                    currentYRotation,
                    zTilt
                );

            if (currentSpinSpeed <= fallSpinThreshold)
            {
                FallDown();
            }
        }

        private void FallDown()
        {
            isFallen = true;

            transform.rotation =
                startRotation *
                Quaternion.Euler(
                    90f,
                    currentYRotation,
                    0f
                );
        }
    }
}