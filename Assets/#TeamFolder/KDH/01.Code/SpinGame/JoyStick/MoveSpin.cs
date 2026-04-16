using UnityEngine;

namespace KDH
{
    public class MoveSpin : MonoBehaviour
    {
        [Header("회전")]
        public float spinSpeed = 1000f;

        [Header("흔들림")]
        public float wobbleSpeed = 6f;
        public float wobbleIncreaseSpeed = 0.7f;
        public float maxWobbleAngle = 20f;

        private float currentWobble = 0f;
        private float extraHitWobble = 0f;
        private float currentYRotation = 0f;
        private float timeValue = 0f;

        private void Update()
        {
            SpinAndWobble();
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

            extraHitWobble = Mathf.Lerp(
                extraHitWobble,
                0f,
                Time.deltaTime * 2f
            );

            float totalWobble = currentWobble + extraHitWobble;

            currentYRotation += spinSpeed * Time.deltaTime;

            float xTilt =
                Mathf.Sin(timeValue * wobbleSpeed) * totalWobble;

            float zTilt =
                Mathf.Cos(timeValue * wobbleSpeed) * totalWobble;

            transform.rotation = Quaternion.Euler(
                xTilt,
                currentYRotation,
                zTilt
            );
        }

        public void AddHitWobble(float amount)
        {
            extraHitWobble += amount;
        }
    }
}