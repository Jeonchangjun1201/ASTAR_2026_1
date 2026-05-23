using System;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 플레이어 물리 이동·점프·스태미나·지면 판정 전담 컴포넌트.
    /// <see cref="PlayerController"/>는 입력/비주얼/게임 상태만 맡고 FixedUpdate 물리는 여기서 처리한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class PlayerMovementController : MonoBehaviour
    {
        public const float SpawnGrace = 0.6f;
        public const float FallAirborneDelay = 0.25f;
        public const float JumpBufferTime = 0.15f;
        public const float CoyoteTime = 0.12f;
        public const float JumpLockout = 0.25f;

        [Header("이동")]
        [SerializeField] private float _moveSpeed = 4.5f;
        [SerializeField] private float _sprintMultiplier = 1.3f;
        [SerializeField] private float _jumpForce = 6f;
        [SerializeField] private float _rotationSpeed = 15f;
        [Tooltip("Y 속도가 이 값보다 낮으면 낙하(점프 아님)로 본다.")]
        [SerializeField] private float _fallVelocityThreshold = -0.5f;

        [Header("스태미나")]
        [SerializeField] private float _maxStamina = 100f;
        [SerializeField] private float _sprintDrainPerSec = 30f;
        [SerializeField] private float _staminaRegenPerSec = 20f;
        [SerializeField] private float _minStaminaToSprint = 10f;
        [Tooltip("스태미나가 0이 된 뒤, 이 시간(초)이 지나야 다시 스프린트 시작 가능(회복은 계속).")]
        [SerializeField] private float _sprintReenableDelay = 2f;
        [Tooltip("스태미나 완전 소진 직후 이 시간(초) 동안 이동 속도 패널티.")]
        [SerializeField] private float _exhaustionDuration = 1.4f;
        [Tooltip("탈진 중 이동 속도 배율(걷기·달리기 공통). 0.5 = 절반.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _exhaustionMoveSpeedMul = 0.45f;

        [Header("지면 판정")]
        [Tooltip("캡슐 아래쪽으로 이 거리만큼 더 내려가도 지면으로 친다.")]
        [SerializeField] private float _groundCheckDist = 0.3f;
        [SerializeField] private LayerMask _groundLayer;
        [Tooltip("_groundLayer가 비어있으면 Player 레이어 제외 전체로 폴백")]
        [SerializeField] private bool _fallbackAllButPlayer = true;

        [Header("카메라")]
        [Tooltip("카메라 기준 이동 방향 계산용 — Main Camera Transform 연결")]
        [SerializeField] private Transform _cameraTransform;

        public float Stamina { get; private set; }
        public float MaxStamina => _maxStamina;
        public bool IsSprinting { get; private set; }
        public float MoveSpeed => _moveSpeed;
        public float SprintMultiplier => _sprintMultiplier;
        public bool IsGrounded => _airborne.IsGrounded;

        public event Action JumpPerformed;

        private Rigidbody _rb;
        private Collider _collider;
        private PlayerAirborneState _airborne;
        private float _jumpBufferedUntil;
        private float _lastGroundedTime = -999f;
        private float _jumpCooldownUntil;
        private bool _staminaWasAbove;
        private float _sprintAvailableTime;
        private float _exhaustedUntil;
        private bool _jumpEnabled = true;
        private float _externalSpeedMul = 1f;
        private float _externalSpeedUntil;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            if (_groundLayer.value == 0 && _fallbackAllButPlayer)
                _groundLayer = Physics.DefaultRaycastLayers;

            Stamina = _maxStamina;
            _sprintAvailableTime = 0f;
            _airborne.OnSpawned();
        }

        public void EnsureCameraReference()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        public void SetCameraTransform(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        public void ResetSpawnState()
        {
            _airborne.OnSpawned();
        }

        public void BufferJump()
        {
            if (!_jumpEnabled) return;
            _jumpBufferedUntil = Time.time + JumpBufferTime;
        }

        public void SetJumpEnabled(bool enabled)
        {
            _jumpEnabled = enabled;
            if (!enabled) _jumpBufferedUntil = 0f;
        }

        public void ApplyExternalSlow(float speedRatio, float duration)
        {
            if (duration <= 0f) return;
            _externalSpeedMul = Mathf.Clamp01(Mathf.Max(0f, speedRatio));
            _externalSpeedUntil = Time.time + duration;
        }

        public void RefillStamina(float amount)
        {
            if (amount <= 0f) return;
            Stamina = Mathf.Min(_maxStamina, Stamina + amount);
            if (Stamina >= _minStaminaToSprint)
            {
                _sprintAvailableTime = 0f;
                _exhaustedUntil = 0f;
            }
        }

        public void ClearSprintState()
        {
            IsSprinting = false;
        }

        /// <summary>Update에서 지면·공중 상태를 갱신하고 착지/낙하 비주얼 이벤트를 반환한다.</summary>
        public PlayerAirborneVisualDelta TickAirborne(bool respectJumpCooldown)
        {
            bool grounded = respectJumpCooldown
                ? Time.time >= _jumpCooldownUntil && CheckGround()
                : CheckGround();

            float velocityY = _rb != null ? _rb.linearVelocity.y : 0f;
            return PlayerControllerAirborneModule.Tick(
                ref _airborne,
                grounded,
                velocityY,
                _fallVelocityThreshold,
                SpawnGrace,
                FallAirborneDelay);
        }

        /// <summary>
        /// FixedUpdate 물리 이동. canMove가 false면 마찰만 적용한다.
        /// </summary>
        public bool ProcessFixedStep(PlayerMovementInput input, bool canMove)
        {
            if (_rb != null && _rb.isKinematic) return false;

            if (!canMove)
            {
                PlayerControllerMovementModule.ApplyFriction(_rb);
                return false;
            }

            UpdateStamina(input);
            ApplyMovement(input);

            if (_airborne.IsGrounded) _lastGroundedTime = Time.time;

            bool wantsJump = Time.time <= _jumpBufferedUntil;
            bool canStillJump = (Time.time - _lastGroundedTime) <= CoyoteTime;
            if (!_jumpEnabled || !wantsJump || !canStillJump) return false;

            PlayerControllerMovementModule.ApplyJump(_rb, _jumpForce);
            _jumpBufferedUntil = 0f;
            _lastGroundedTime = -999f;
            _jumpCooldownUntil = Time.time + JumpLockout;
            JumpPerformed?.Invoke();
            return true;
        }

        private void UpdateStamina(PlayerMovementInput input)
        {
            float stamina = Stamina;
            bool isSprinting = IsSprinting;
            PlayerControllerMovementModule.UpdateStamina(
                ref stamina,
                _maxStamina,
                _sprintDrainPerSec,
                _staminaRegenPerSec,
                _minStaminaToSprint,
                _sprintReenableDelay,
                _exhaustionDuration,
                input.SprintHeld,
                input.Move,
                ref isSprinting,
                ref _sprintAvailableTime,
                ref _exhaustedUntil,
                ref _staminaWasAbove);
            Stamina = stamina;
            IsSprinting = isSprinting;
        }

        private void ApplyMovement(PlayerMovementInput input)
        {
            EnsureCameraReference();
            PlayerControllerMovementModule.ApplyMovement(
                _rb,
                transform,
                input.Move,
                _cameraTransform,
                _moveSpeed,
                _sprintMultiplier,
                IsSprinting,
                PlayerControllerMovementModule.ResolveExternalSpeedMul(_externalSpeedUntil, _externalSpeedMul),
                PlayerControllerMovementModule.ResolveExhaustionSpeedMul(_exhaustedUntil, _exhaustionMoveSpeedMul),
                _rotationSpeed);
        }

        private bool CheckGround()
        {
            return PlayerControllerMovementModule.CheckGround(_collider, transform, _groundCheckDist, _groundLayer);
        }
    }
}
