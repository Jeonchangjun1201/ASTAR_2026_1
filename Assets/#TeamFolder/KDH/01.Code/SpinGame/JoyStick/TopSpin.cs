using UnityEngine;

namespace KDH
{
    public class TopSpin : MonoBehaviour
    {
        [Header("회전")]
        [SerializeField] private float startSpinSpeed = 1000f;
        [SerializeField] private float slowDownSpeed = 20f;

        [Header("흔들림")]
        [SerializeField] private float wobbleSpeed = 4f;
        [SerializeField] private float maxWobbleAngle = 25f;

        [Header("넘어짐 판정")]
        [SerializeField] private float fallSpinThreshold = 80f;
        [SerializeField] private float fallTiltAngle = 60f;

        private float _currentSpinSpeed;
        private float _currentYRotation = 0f;
        private float _timeValue = 0f;
        private bool _isFallen = false;
        private TopPlayer _topPlayer;
        private Rigidbody _rb;

        public static event System.Action<string> OnTopFallen;

        private void Awake()
        {
            _currentSpinSpeed = startSpinSpeed;
            _topPlayer = GetComponent<TopPlayer>();
            _rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (_isFallen) return;
            Spin();
            CheckFall();
        }

        private void Spin()
        {
            _timeValue += Time.deltaTime;

            _currentSpinSpeed -= slowDownSpeed * Time.deltaTime;
            _currentSpinSpeed = Mathf.Max(_currentSpinSpeed, 0f);

            float speedRatio = _currentSpinSpeed / startSpinSpeed;
            float wobbleAmount = (1f - speedRatio * speedRatio) * maxWobbleAngle;

            _currentYRotation += _currentSpinSpeed * Time.deltaTime;

            float xTilt = Mathf.Sin(_timeValue * wobbleSpeed) * wobbleAmount;
            float zTilt = Mathf.Cos(_timeValue * wobbleSpeed) * wobbleAmount;

            // localEulerAngles로 변경!
            _rb.MoveRotation(Quaternion.Euler(xTilt, _currentYRotation, zTilt));
        }

        private void CheckFall()
        {
            if (_currentSpinSpeed <= fallSpinThreshold)
            {
                FallDown();
                return;
            }

            float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
            if (tiltAngle > fallTiltAngle)
                FallDown();
        }

        private void FallDown()
        {
            _isFallen = true;

            transform.rotation = Quaternion.Euler(90f, _currentYRotation, 0f);

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            if (_topPlayer != null)
                _topPlayer.enabled = false;

            OnTopFallen?.Invoke(gameObject.name);
            Debug.Log($"{gameObject.name} 넘어짐!");
        }
    }
}