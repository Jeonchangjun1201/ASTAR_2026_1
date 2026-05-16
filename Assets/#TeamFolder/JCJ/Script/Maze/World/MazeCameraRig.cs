using UnityEngine;

// 플레이어 주위를 도는 카메라 리그와 회전을 관리하는 처리.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 플레이어 월드 위치를 따라가고 마우스 누적 입력으로 회전하는 피벗.
    /// Cinemachine vcam이 LockToTarget으로 이 피벗을 따라 요/피치가 곧바로 카메라에 반영된다.
    /// </summary>
    public class MazeCameraRig : MonoBehaviour
    {
        public static MazeCameraRig Instance { get; private set; }

        private Transform _target;
        private bool  _enabled   = true;
        private bool  _allowPitch = false;
        private float _minPitch  = -30f;
        private float _maxPitch  =  55f;
        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 리그 중복 시 첫 번째만 유지 — 남은 복제가 댐핑 설정을 덮어쓰지 않게.
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Configure(bool enabled, float minPitch, float maxPitch, bool allowPitch = false)
        {
            _enabled    = enabled;
            _allowPitch = allowPitch;
            _minPitch   = minPitch;
            _maxPitch   = maxPitch;
            _pitch      = 0f;
        }

        public void SetAllowPitch(bool allow)
        {
            _allowPitch = allow;
            if (!allow) _pitch = 0f;
        }

        public bool IsPitchAllowed => _allowPitch;
        public float Yaw => _yaw;
        public float Pitch => _pitch;

        public void SetPitch(float pitch)
        {
            _pitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
        }

        // 카메라 피벗이 따라갈 플레이어를 바꾼다.
        // 관전 전환이나 로컬 소유 플레이어 변경 시점에 연결되는 메서드다.
        public void SetTarget(Transform target)
        {
            _target = target;
            if (target != null)
            {
                transform.position = target.position;
                _yaw = target.eulerAngles.y;
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
        }

        /// <summary>Accumulate mouse delta (already multiplied by sensitivity).</summary>
        // 마우스 입력 누적값을 요/피치 값으로 변환하는 지점이다.
        // 네트워크 동기화 대상이라기보다 순수 로컬 카메라 입력이라는 점을 여기서 보면 된다.
        public void AddLook(Vector2 delta)
        {
            if (!_enabled) return;
            float sens = 1f;
            var battleCam = _TeamFolder.JCJ.Battle.BattleFirstPersonCamera.Instance;
            if (battleCam != null) sens = battleCam.AimSensitivityMultiplier;
            _yaw += delta.x * sens;
            if (_allowPitch)
            {
                _pitch -= delta.y * sens;
                _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
            }
        }

        private void LateUpdate()
        {
            var battleCam = GetComponent<_TeamFolder.JCJ.Battle.BattleFirstPersonCamera>();
            if (battleCam != null && battleCam.enabled && battleCam.FollowTarget != null)
            {
                if (battleCam.SuspendAutomaticCameraFollow) return;
                return;
            }

            if (_target != null) transform.position = _target.position;
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        /// <summary>카메라 리그가 향하는 월드 전방(요만, 수평).</summary>
        public Vector3 GetYawForward()
        {
            return Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
        }

        public Vector3 GetYawRight()
        {
            return Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
        }
    }
}
