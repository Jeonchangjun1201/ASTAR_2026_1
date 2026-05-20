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
        private TopPlayer _topPlayer; // JHJPlayerController 대신 TopPlayer로 변경!

        public static event System.Action<string> OnTopFallen;

        private void Awake()
        {
            currentSpinSpeed = startSpinSpeed;
            startRotation = transform.rotation;
            _topPlayer = GetComponent<TopPlayer>(); // 같은 오브젝트에서 찾기
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
            float wobbleAmount = (1f - speedRatio * speedRatio) * maxWobbleAngle;

            currentYRotation += currentSpinSpeed * Time.deltaTime;

            float xTilt = Mathf.Sin(timeValue * wobbleSpeed) * wobbleAmount;
            float zTilt = Mathf.Cos(timeValue * wobbleSpeed) * wobbleAmount;

            transform.rotation = startRotation * Quaternion.Euler(xTilt, currentYRotation, zTilt);

            if (currentSpinSpeed <= fallSpinThreshold)
                FallDown();
        }

        private void FallDown()
        {
            isFallen = true;

            transform.rotation = startRotation * Quaternion.Euler(90f, currentYRotation, 0f);

            // TopPlayer 끄기 → 조이스틱 입력 막힘
            if (_topPlayer != null)
                _topPlayer.enabled = false;

            OnTopFallen?.Invoke(gameObject.name);
            Debug.Log($"{gameObject.name} 넘어짐!");
        }
    }
}