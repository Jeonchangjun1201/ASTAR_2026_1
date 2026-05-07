using UnityEngine;
using UnityEngine.InputSystem;

namespace _TeamFolder.JCJ.Script
{
    [RequireComponent(typeof(Rigidbody))]
    public partial class PlayerController : MonoBehaviour
    {
        [Header("식별")]
        [Tooltip("true = 키보드 입력 받음(로컬). false = 원격/AI(가만히 있음, 잡기 결과만 받음).")]
        [SerializeField] private bool _isLocalControlled = true;

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
        [SerializeField] private float _sprintReenableDelay = 1f;

        [Header("지면 판정")]
        [Tooltip("캡슐 아래쪽으로 이 거리만큼 더 내려가도 지면으로 친다.")]
        [SerializeField] private float _groundCheckDist = 0.3f;
        [SerializeField] private LayerMask _groundLayer;
        [Tooltip("_groundLayer가 비어있으면 Player 레이어 제외 전체로 폴백")]
        [SerializeField] private bool _fallbackAllButPlayer = true;

        [Header("마우스 룩")]
        [SerializeField] private bool _enableMouseLook = true;
        [SerializeField] private float _mouseSensitivity = 0.18f;
        [SerializeField] private bool _lockCursor = true;

        [Header("카메라")]
        [Tooltip("카메라 기준 이동 방향 계산용 — Main Camera Transform 연결")]
        [SerializeField] private Transform _cameraTransform;

        [Header("비주얼(선택)")]
        [SerializeField] private bool _addTrailIfMissing;
        [Tooltip("PartyCharacters 리깅 비주얼(권장). 끄거나 프리팹 없으면 절차적 프리미티브로 폴백.")]
        [SerializeField] private bool _usePartyCharacter = true;

        [Header("효과음")]
        [Tooltip("걷기 속도에서 발소리 간격(초). 스프린트는 더 짧아짐.")]
        [SerializeField] private float _walkFootstepInterval = 0.42f;
        [SerializeField] private float _sprintFootstepInterval = 0.27f;

        public float Stamina { get; private set; }
        public float MaxStamina => _maxStamina;
        public bool IsSprinting { get; private set; }
        public float MoveSpeed => _moveSpeed;
        public bool IsSpectating { get; private set; }

        private float _externalSpeedMul = 1f;
        private float _externalSpeedUntil;

        private Rigidbody _rb;
        private Collider _collider;
        private static PhysicsMaterial _lowFrictionPlayerMaterial;
        private InputActionMap _inputMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _lookAction;
        private Vector2 _lookInput;
        private Vector2 _moveInput;
        private bool _sprintHeld;
        private bool _isGrounded;
        private bool _wasGrounded;
        private bool _isFalling;
        private float _spawnTime;
        private float _airborneSince;
        private float _jumpBufferedUntil;
        private float _lastGroundedTime = -999f;
        private float _jumpCooldownUntil;
        private IPlayerVisual _visual;
        private float _nextFootstepTime;
        private bool _staminaWasAbove;
        private float _sprintAvailableTime;
        private bool _lookInputEnabled = true;

        private const float SpawnGrace = 0.6f;
        private const float FallAirborneDelay = 0.25f;
        private const float JumpBufferTime = 0.15f;
        private const float CoyoteTime = 0.12f;
        private const float JumpLockout = 0.25f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _collider = GetComponent<Collider>();
            ApplyLowFrictionMaterial();

            Stamina = _maxStamina;
            _sprintAvailableTime = 0f;

            if (_groundLayer.value == 0 && _fallbackAllButPlayer)
                _groundLayer = Physics.DefaultRaycastLayers;

            _spawnTime = Time.time;
            _isGrounded = true;
            _wasGrounded = true;

            // 입력 바인딩은 로컬 소유 플레이어만 활성화해야 한다.
            // 서버 연결 후에는 "내 플레이어인지" 판정이 끝난 뒤 IsLocalControlled를 세팅하는 진입점이 된다.
            BuildInputActions();

            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            if (_addTrailIfMissing && GetComponent<TrailRenderer>() == null)
                AddDefaultTrail();

            _visual = GetComponent<IPlayerVisual>();
            if (_visual == null) _visual = AttachPreferredVisual();
            HideBasePrimitiveMesh();
        }

        private void OnEnable()
        {
            if (_isLocalControlled) _inputMap?.Enable();
            else _inputMap?.Disable();

            if (_isLocalControlled && _enableMouseLook && _lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            _inputMap?.Disable();

            if (_lockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }

        private void OnDestroy()
        {
            _inputMap?.Disable();
            _inputMap?.Dispose();
        }

        private void Update()
        {
            if (!_isLocalControlled)
            {
                // 원격 플레이어는 입력을 읽지 않는다.
                // 서버 권위 이동을 붙일 때도 원격 오브젝트는 동기화 결과만 반영해야 안전하다.
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
                return;
            }

            _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            _lookInput = _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
            _isGrounded = Time.time >= _jumpCooldownUntil && CheckGround();

            bool inGrace = (Time.time - _spawnTime) < SpawnGrace;

            if (_isGrounded)
            {
                _airborneSince = 0f;
                if (!_wasGrounded && _isFalling && !inGrace) _visual?.OnLand();
                _isFalling = false;
            }
            else
            {
                if (_airborneSince <= 0f) _airborneSince = Time.time;
                float airborneTime = Time.time - _airborneSince;
                if (!_isFalling
                    && !inGrace
                    && airborneTime > FallAirborneDelay
                    && _rb.linearVelocity.y < _fallVelocityThreshold)
                {
                    _visual?.OnFall();
                    _isFalling = true;
                }
            }

            _wasGrounded = _isGrounded;
            DispatchMouseLook();
            UpdateVisualState();
        }

        private void FixedUpdate()
        {
            if (_rb != null && _rb.isKinematic) return;

            if (!_isLocalControlled)
            {
                // 원격 플레이어는 물리 이동도 처리하지 않는다.
                // 서버가 위치/속도를 authoritative 하게 내려주면 그 값만 적용하는 자리다.
                ClearInputState();
                return;
            }

            var gsm = GameStateManager.Instance;
            if (gsm != null && gsm.CurrentState != GameState.Playing)
            {
                ApplyFriction();
                return;
            }

            UpdateStamina();
            ApplyMovement();

            if (_isGrounded) _lastGroundedTime = Time.time;
            bool wantsJump = Time.time <= _jumpBufferedUntil;
            bool canStillJump = (Time.time - _lastGroundedTime) <= CoyoteTime;
            if (wantsJump && canStillJump)
            {
                ApplyJump();
                _visual?.OnJump();
                MazeAudio.Play(MazeSfx.Jump);
                _jumpBufferedUntil = 0f;
                _lastGroundedTime = -999f;
                _jumpCooldownUntil = Time.time + JumpLockout;
            }

            UpdateFootstepSfx();
        }

        public bool IsLocalControlled
        {
            get => _isLocalControlled;
            set
            {
                // 서버 연결 뒤 소유권 확정 결과를 반영하는 핵심 플래그다.
                // true인 개체만 입력, 점프 버퍼, 마우스 룩을 허용한다.
                _isLocalControlled = value;
                ApplyLocalControlState();
                if (!_isLocalControlled) ClearInputState();
            }
        }

        private void BuildInputActions()
        {
            PlayerControllerInputModule.BuildInputActions(
                () => _isLocalControlled,
                // 서버 입력 패킷을 보낼 구조로 바꿀 때도 점프 버퍼 생성 시점은 여기서 유지하면 된다.
                () => _jumpBufferedUntil = Time.time + JumpBufferTime,
                held => _sprintHeld = held,
                out _inputMap,
                out _moveAction,
                out _jumpAction,
                out _sprintAction,
                out _lookAction);
        }

        public InputActionMap GetInputMap() => _inputMap;

        private void ApplyLocalControlState()
        {
            PlayerControllerInputModule.ApplyLocalControlState(_isLocalControlled, _inputMap);
        }

        private void DispatchMouseLook()
        {
            if (!_lookInputEnabled) return;
            PlayerControllerInputModule.DispatchMouseLook(_enableMouseLook, _lookInput, _mouseSensitivity);
        }

        public void SetMouseSensitivity(float value)
        {
            _mouseSensitivity = Mathf.Clamp(value, 0.01f, 2f);
        }

        public void SetMovementEnabled(bool enabled)
        {
            PlayerControllerInputModule.SetMovementEnabled(enabled, _moveAction, _jumpAction, _sprintAction);
        }

        public void SetLookEnabled(bool enabled)
        {
            _lookInputEnabled = enabled;
            if (!enabled) _lookInput = Vector2.zero;
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            SetMovementEnabled(enabled);
            SetLookEnabled(enabled);
            if (!enabled) ClearInputState();
        }

        private void ClearInputState()
        {
            bool isSprinting = IsSprinting;
            PlayerControllerInputModule.ClearInputState(ref _moveInput, ref _lookInput, ref _sprintHeld, ref isSprinting, ref _jumpBufferedUntil);
            IsSprinting = isSprinting;
        }

        private void OnGameStateChanged(GameState state)
        {
            bool canMove = state == GameState.Playing;
            SetMovementEnabled(canMove);

            if (_lockCursor)
            {
                bool shouldLock = state == GameState.Playing || state == GameState.Countdown;
                Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !shouldLock;
            }
        }

        public void ApplyExternalSlow(float speedRatio, float duration)
        {
            if (duration <= 0f) return;
            // 현재는 로컬 상태 변화지만 서버 게임에서는 감속 시작/종료 시각을 서버 기준으로 동기화해야 한다.
            _externalSpeedMul = Mathf.Clamp01(Mathf.Max(0f, speedRatio));
            _externalSpeedUntil = Time.time + duration;
        }

        public void RefillStamina(float amount)
        {
            if (amount <= 0f) return;
            Stamina = Mathf.Min(_maxStamina, Stamina + amount);
            if (Stamina >= _minStaminaToSprint)
                _sprintAvailableTime = 0f;
        }

        private float ResolveExternalSpeedMul()
        {
            return PlayerControllerMovementModule.ResolveExternalSpeedMul(_externalSpeedUntil, _externalSpeedMul);
        }

        private void UpdateStamina()
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
                _sprintHeld,
                _moveInput,
                ref isSprinting,
                ref _sprintAvailableTime,
                ref _staminaWasAbove);
            Stamina = stamina;
            IsSprinting = isSprinting;
        }

        private void ApplyMovement()
        {
            PlayerControllerMovementModule.ApplyMovement(
                _rb,
                transform,
                _moveInput,
                _cameraTransform,
                _moveSpeed,
                _sprintMultiplier,
                IsSprinting,
                ResolveExternalSpeedMul(),
                _rotationSpeed);
        }

        private void ApplyFriction()
        {
            PlayerControllerMovementModule.ApplyFriction(_rb);
        }

        private void ApplyJump()
        {
            PlayerControllerMovementModule.ApplyJump(_rb, _jumpForce);
        }

        private Vector3 GetMoveDirection()
        {
            return PlayerControllerMovementModule.GetCameraRelativeDirection(_moveInput, _cameraTransform);
        }

        private Vector3 GetCameraRelativeDirection()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
            return PlayerControllerMovementModule.GetCameraRelativeDirection(_moveInput, _cameraTransform);
        }

        private bool CheckGround()
        {
            return PlayerControllerMovementModule.CheckGround(_collider, transform, _groundCheckDist, _groundLayer);
        }

        public void SetSpectating(bool spectating)
        {
            // 관전 전환은 입력 잠금과 UI 표시 여부를 함께 바꾸는 신호다.
            // 서버 기준 탈락/완주 이벤트를 받았을 때 이 함수를 호출하도록 맞추면 된다.
            IsSpectating = spectating;
            if (spectating) ClearInputState();
        }

        public void NotifyCollected()
        {
            _visual?.OnCollect();
        }

        private IPlayerVisual AttachPreferredVisual()
        {
            return PlayerControllerPresentationModule.AttachPreferredVisual(this, _usePartyCharacter);
        }

        private void ApplyLowFrictionMaterial()
        {
            PlayerControllerPresentationModule.ApplyLowFrictionMaterial(_collider, ref _lowFrictionPlayerMaterial);
        }

        private void HideBasePrimitiveMesh()
        {
            PlayerControllerPresentationModule.HideBasePrimitiveMesh(this);
        }

        private void AddDefaultTrail()
        {
            PlayerControllerPresentationModule.AddDefaultTrail(this);
        }

        private void UpdateFootstepSfx()
        {
            PlayerControllerPresentationModule.UpdateFootstepSfx(
                _isGrounded,
                _moveInput,
                IsSprinting,
                _walkFootstepInterval,
                _sprintFootstepInterval,
                ref _nextFootstepTime);
        }

        private void UpdateVisualState()
        {
            PlayerControllerPresentationModule.UpdateVisualState(_visual, _isGrounded, _moveInput, IsSprinting);
        }
    }
}
