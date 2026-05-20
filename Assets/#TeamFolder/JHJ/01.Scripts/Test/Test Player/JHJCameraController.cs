using Unity.Cinemachine;
using UnityEngine;
using JHJ.Test.TestPlayer;

namespace JHJ.Scripts.Test.TestPlayer
{
    public class JHJCameraController : MonoBehaviour
    {
        [Header("시네머신 카메라")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;

        [Header("추적할 플레이어 (캐릭터 최상위 Transform)")]
        [SerializeField] private Transform _playerTransform;

        [Header("인풋 리더")]
        [SerializeField] private InputReader _inputReader;

        [Header("시점 세팅")]
        [SerializeField] private float _pivotHeight = 1.3f;
        [SerializeField] private float _cameraDistance = 3.5f;
        [SerializeField] private float _shoulderOffset = 0.5f; // 배그 느낌을 위한 우측 어깨 오프셋 (원신은 0 추천)

        [Header("회전 감도")]
        [SerializeField] private float _horizontalSensitivity = 15f;
        [SerializeField] private float _verticalSensitivity = 10f;

        [Header("위아래 각도 제한")]
        [SerializeField] private float _minPitch = -30f;
        [SerializeField] private float _maxPitch = 50f;

        [Header("회전 부드러움 (쫀득한 손맛)")]
        [SerializeField] private float _rotationSmoothTime = 0.03f;

        [Header("카메라 충돌 (벽 뚫기 방지)")]
        [SerializeField] private LayerMask _collisionMask;
        [SerializeField] private float _collisionRadius = 0.2f;

        private float _yaw;
        private float _pitch = 10f;

        private float _currentYaw;
        private float _currentPitch;
        private float _yawVelocity;
        private float _pitchVelocity;
        private Vector2 _lookDelta;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (_cinemachineCamera != null)
            {
                _cinemachineCamera.Target.TrackingTarget = null;
                _cinemachineCamera.Target.LookAtTarget = null;
            }

            if (_playerTransform != null) _currentYaw = _yaw = _playerTransform.eulerAngles.y;
            _currentPitch = _pitch;
        }

        private void OnEnable()
        {
            if (_inputReader != null) _inputReader.P1OnLook += OnLook;
        }

        private void OnDisable()
        {
            if (_inputReader != null) _inputReader.P1OnLook -= OnLook;
        }

        private void OnLook(Vector2 delta) => _lookDelta = delta;

        private void LateUpdate()
        {
            if (_playerTransform == null || _cinemachineCamera == null) return;

            // 회전 계산
            _yaw += _lookDelta.x * _horizontalSensitivity * Time.deltaTime;
            _pitch -= _lookDelta.y * _verticalSensitivity * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _yaw, ref _yawVelocity, _rotationSmoothTime);
            _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _pitch, ref _pitchVelocity, _rotationSmoothTime);

            Quaternion cameraRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);

            // 중심축 계산 및 숄더 오프셋 적용
            Vector3 pivotPosition = _playerTransform.position + Vector3.up * _pivotHeight;
            pivotPosition += cameraRotation * Vector3.right * _shoulderOffset;

            // 거리 및 벽 충돌 계산
            Vector3 targetCamPosition = pivotPosition - (cameraRotation * Vector3.forward * _cameraDistance);

            if (Physics.SphereCast(pivotPosition, _collisionRadius, -(cameraRotation * Vector3.forward), out RaycastHit hit, _cameraDistance, _collisionMask))
            {
                targetCamPosition = pivotPosition - (cameraRotation * Vector3.forward * Mathf.Max(hit.distance - _collisionRadius, 0.5f));
            }

            _cinemachineCamera.transform.position = targetCamPosition;
            _cinemachineCamera.transform.rotation = cameraRotation;

            _lookDelta = Vector2.zero;
        }
    }
}