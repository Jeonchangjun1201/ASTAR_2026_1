using UnityEngine;
using UnityEngine.InputSystem;

namespace _TeamFolder.JCJ.Script
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("식별")]
        [Tooltip("true = 키보드 입력 받음(로컬). false = 원격/AI(가만히 있음, 잡기 결과만 받음).")]
        [SerializeField] private bool _isLocalControlled = true;

        [Header("이동")]
        [SerializeField] private float _moveSpeed       = 4.5f;
        [SerializeField] private float _sprintMultiplier = 1.3f;
        [SerializeField] private float _jumpForce       = 6f;
        [SerializeField] private float _rotationSpeed   = 15f;
        [Tooltip("Y 속도가 이 값보다 낮으면 낙하(점프 아님)로 본다.")]
        [SerializeField] private float _fallVelocityThreshold = -0.5f;

        [Header("스태미나")]
        [SerializeField] private float _maxStamina        = 100f;
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
        [SerializeField] private bool  _enableMouseLook  = true;
        [SerializeField] private float _mouseSensitivity = 0.18f;
        [SerializeField] private bool  _lockCursor       = true;

        [Header("카메라")]
        [Tooltip("카메라 기준 이동 방향 계산용 — Main Camera Transform 연결")]
        [SerializeField] private Transform _cameraTransform;

        [Header("비주얼(선택)")]
        [SerializeField] private bool _addTrailIfMissing = false;
        [Tooltip("PartyCharacters 리깅 비주얼(권장). 끄거나 프리팹 없으면 절차적 프리미티브로 폴백.")]
        [SerializeField] private bool _usePartyCharacter = true;

        [Header("효과음")]
        [Tooltip("걷기 속도에서 발소리 간격(초). 스프린트는 더 짧아짐.")]
        [SerializeField] private float _walkFootstepInterval   = 0.42f;
        [SerializeField] private float _sprintFootstepInterval = 0.27f;

        public float Stamina { get; private set; }
        public float MaxStamina => _maxStamina;
        public bool IsSprinting { get; private set; }
        public float MoveSpeed => _moveSpeed;
        public bool IsSpectating { get; private set; }

        private float _externalSpeedMul = 1f;
        private float _externalSpeedUntil;

        private Rigidbody  _rb;
        private Collider   _collider;
        private static PhysicsMaterial _lowFrictionPlayerMaterial;
        private InputActionMap _inputMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _lookAction;
        private Vector2     _lookInput;

        private Vector2 _moveInput;
        private bool    _sprintHeld;
        private bool    _isGrounded;
        private bool    _wasGrounded;
        private bool    _isFalling;
        private float   _spawnTime;
        private float   _airborneSince;
        private float   _jumpBufferedUntil;
        private float   _lastGroundedTime = -999f;
        private float   _jumpCooldownUntil;
        private IPlayerVisual _visual;

        private float   _nextFootstepTime;
        private bool    _staminaWasAbove;
        /// <summary>이 시각까지 스프린트 시작 불가(완전 소진 후).</summary>
        private float   _sprintAvailableTime;

        // 스폰 직후 중력 안정 유예 — 이전에는 낙하 애니·착지 알림 금지.
        private const float SpawnGrace = 0.6f;
        // 튀어 오른 한 프레임이 아니라 실제 공중 시간이 있어야 낙하 애니.
        private const float FallAirborneDelay = 0.25f;
        // 착지 직전 점프 입력을 이 시간만큼 기억.
        private const float JumpBufferTime = 0.15f;
        // 발이 떠진 뒤에도 이 시간 안이면 코요테 점프 허용.
        private const float CoyoteTime = 0.12f;
        // 점프 직후 이 시간 동안은 지면 판정 무시 — 아직 땅으로 치는 레이로 연점프 방지.
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
            {
                // IgnoreRaycast만 제외, Default는 유지 — 플레이어가 Default에 있어도 바닥·벽 검출.
                _groundLayer = Physics.DefaultRaycastLayers;
            }

            _spawnTime = Time.time;
            _isGrounded = true;
            _wasGrounded = true;

            BuildInputActions();

            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            if (_addTrailIfMissing && GetComponent<TrailRenderer>() == null)
                AddDefaultTrail();

            _visual = GetComponent<IPlayerVisual>();
            if (_visual == null) _visual = AttachPreferredVisual();
            HideBasePrimitiveMesh();
        }

        private IPlayerVisual AttachPreferredVisual()
        {
            if (_usePartyCharacter)
            {
                var party = gameObject.AddComponent<PartyCharacterVisual>();
                return party;
            }
            return gameObject.AddComponent<PlayerVisualController>();
        }

        private void ApplyLowFrictionMaterial()
        {
            if (_collider == null) return;
            _lowFrictionPlayerMaterial ??= new PhysicsMaterial("MazePlayerLowFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            _collider.sharedMaterial = _lowFrictionPlayerMaterial;
        }

        private void HideBasePrimitiveMesh()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        public void NotifyCollected()
        {
            _visual?.OnCollect();
        }

        private void AddDefaultTrail()
        {
            var tr = gameObject.AddComponent<TrailRenderer>();
            tr.time = 0.6f;
            tr.startWidth = 0.25f;
            tr.endWidth = 0.02f;
            tr.minVertexDistance = 0.1f;
            tr.emitting = true;

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            var col = new Color(0.4f, 0.9f, 1f, 0.6f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            mat.color = col;
            tr.material = mat;

            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(col, 0f), new GradientColorKey(col, 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = grad;
        }

        private void BuildInputActions()
        {
            // 각 플레이어는 자기 InputActionMap을 가진다.
            // 서버 연동 시에도 "로컬 소유 플레이어"만 Enable하고, 원격 플레이어는 입력을 절대 받지 않게 유지해야 한다.
            _inputMap   = JCJInputActions.CreateMap();
            _moveAction = JCJInputActions.Find(_inputMap, JCJInputActions.ActionMove);
            _jumpAction = JCJInputActions.Find(_inputMap, JCJInputActions.ActionJump);
            _sprintAction = JCJInputActions.Find(_inputMap, JCJInputActions.ActionSprint);
            _lookAction = JCJInputActions.Find(_inputMap, JCJInputActions.ActionLook);

            if (_jumpAction != null)
                _jumpAction.performed += _ =>
                {
                    // 관전 중이거나 원격 플레이어인 경우 점프 버퍼를 만들지 않는다.
                    // 이 가드가 없으면 관전 카메라로 보고 있는 플레이어까지 내 점프 입력에 반응한다.
                    if (!_isLocalControlled) return;
                    _jumpBufferedUntil = Time.time + JumpBufferTime;
                };
            if (_sprintAction != null)
            {
                _sprintAction.started  += _ => { if (_isLocalControlled) _sprintHeld = true; };
                _sprintAction.canceled += _ => _sprintHeld = false;
            }
        }

        public InputActionMap GetInputMap() => _inputMap;

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

        public bool IsLocalControlled
        {
            get => _isLocalControlled;
            set
            {
                // 조작 권한 플래그다.
                // true인 플레이어만 키보드/마우스 입력, 이동, 점프, 스프린트를 처리한다.
                _isLocalControlled = value;
                ApplyLocalControlState();
                if (!_isLocalControlled) ClearInputState();
            }
        }

        private void ApplyLocalControlState()
        {
            if (_inputMap == null) return;
            if (_isLocalControlled) _inputMap.Enable();
            else _inputMap.Disable();
        }

        private void Update()
        {
            // 원격/관전 플레이어는 입력을 읽지 않는다.
            // 단순히 InputAction을 Disable하는 것만으로는 이전 프레임의 점프 버퍼가 남을 수 있어 여기서도 방어한다.
            if (!_isLocalControlled) { _moveInput = Vector2.zero; _lookInput = Vector2.zero; return; }
            _moveInput  = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            _lookInput  = _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
            // 점프 직후 짧게 공중으로 간주 — 바닥 근처에서 지면 레이가 _lastGroundedTime을 갱신하지 않게.
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

        private void DispatchMouseLook()
        {
            if (!_enableMouseLook) return;
            var cam = MazeCameraRig.Instance;
            if (cam == null) return;
            Vector2 scaled = _lookInput * _mouseSensitivity;
            cam.AddLook(scaled);
        }

        public void SetMouseSensitivity(float value)
        {
            _mouseSensitivity = Mathf.Clamp(value, 0.01f, 2f);
        }

        public void SetSpectating(bool spectating)
        {
            // 완주 후 관전 상태 표시용 플래그다.
            // MazeMinimap은 이 값을 보고 이미 탈출한 플레이어 점을 표시하지 않는다.
            IsSpectating = spectating;
            if (spectating) ClearInputState();
        }

        public void ApplyExternalSlow(float speedRatio, float duration)
        {
            if (duration <= 0f) return;
            _externalSpeedMul = Mathf.Clamp01(Mathf.Max(0f, speedRatio));
            _externalSpeedUntil = Time.time + duration;
        }


        private float ResolveExternalSpeedMul()
        {
            if (Time.time >= _externalSpeedUntil) return 1f;
            return Mathf.Clamp01(_externalSpeedMul);
        }

        private void FixedUpdate()
        {
            if (_rb != null && _rb.isKinematic) return;
            // 로컬 조작 권한이 없는 플레이어는 물리 이동/점프를 처리하지 않는다.
            // 네트워크 붙일 때 원격 플레이어는 서버 위치 동기화만 받고 여기 로직은 타지 않는 구조가 안전하다.
            if (!_isLocalControlled)
            {
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
            bool wantsJump   = Time.time <= _jumpBufferedUntil;
            bool canStillJump = (Time.time - _lastGroundedTime) <= CoyoteTime;
            if (wantsJump && canStillJump)
            {
                ApplyJump();
                _visual?.OnJump();
                MazeAudio.Play(MazeSfx.Jump);
                _jumpBufferedUntil = 0f;
                _lastGroundedTime  = -999f;
                _jumpCooldownUntil = Time.time + JumpLockout;
            }

            UpdateFootstepSfx();
        }

        private void ClearInputState()
        {
            // 조작권을 잃는 순간 이전 입력 잔여값을 모두 제거한다.
            // 특히 점프 버퍼가 남으면 관전 전환 직후 원격 플레이어가 한 번 더 점프할 수 있다.
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _sprintHeld = false;
            IsSprinting = false;
            _jumpBufferedUntil = 0f;
        }

        private void UpdateFootstepSfx()
        {
            if (!_isGrounded || _moveInput.sqrMagnitude < 0.04f) return;
            if (Time.time < _nextFootstepTime) return;

            float interval = IsSprinting ? _sprintFootstepInterval : _walkFootstepInterval;
            _nextFootstepTime = Time.time + interval;
            MazeAudio.Play(MazeSfx.Footstep, volumeScale: IsSprinting ? 0.9f : 0.7f,
                           pitch: Random.Range(0.92f, 1.08f));
        }

        private void UpdateVisualState()
        {
            if (_visual == null) return;

            // 공중에서는 idle/run 트리거 금지 — AnyState에서 점프/낙하를 덮어씀.
            if (!_isGrounded) return;

            var gsm = GameStateManager.Instance;
            bool playing = gsm == null || gsm.CurrentState == GameState.Playing;
            if (!playing || _moveInput.sqrMagnitude < 0.01f)
            {
                _visual.OnIdle();
                return;
            }

            float speedNorm = Mathf.Clamp01(_moveInput.magnitude);
            if (IsSprinting) _visual.OnSprint(speedNorm);
            else             _visual.OnWalk(speedNorm);
        }

        private void UpdateStamina()
        {
            bool hasInput = _moveInput.sqrMagnitude > 0.01f;

            // 스태미나 남아 있을 때만 스프린트 유지; 새로 시작하려면 잔량+쿨다운 충족(_minStaminaToSprint=0 등 남용 방지).
            bool cooldownDone = Time.time >= _sprintAvailableTime;
            bool canContinue    = IsSprinting && Stamina > 0f;
            // 실제 잔량 필요(인스펙터에서 _minStaminaToSprint==0 대비).
            bool canStartFresh  = cooldownDone && Stamina > 0.01f && Stamina >= _minStaminaToSprint;
            bool canSprint      = canContinue || canStartFresh;
            IsSprinting         = _sprintHeld && hasInput && canSprint;

            if (IsSprinting)
            {
                Stamina -= _sprintDrainPerSec * Time.fixedDeltaTime;
                if (Stamina <= 0f)
                {
                    Stamina = 0f;
                    IsSprinting = false;
                    _sprintAvailableTime = Time.time + Mathf.Max(0f, _sprintReenableDelay);
                    if (_staminaWasAbove) MazeAudio.Play(MazeSfx.StaminaOut, 0.9f);
                }
            }
            else
            {
                Stamina = Mathf.Min(_maxStamina, Stamina + _staminaRegenPerSec * Time.fixedDeltaTime);
            }

            _staminaWasAbove = Stamina > 0.01f;
        }

        private void ApplyMovement()
        {
            Vector3 direction = GetMoveDirection();
            float speed = _moveSpeed * (IsSprinting ? _sprintMultiplier : 1f) * ResolveExternalSpeedMul();

            Vector3 velocity = direction * speed;
            velocity.y = _rb.linearVelocity.y;
            _rb.linearVelocity = velocity;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    Time.fixedDeltaTime * _rotationSpeed);
            }
        }

        private void ApplyFriction()
        {
            var v = _rb.linearVelocity;
            v.x = 0f; v.z = 0f;
            _rb.linearVelocity = v;
        }

        private void ApplyJump()
        {
            // VelocityChange는 질량 무시 — 프리팹 질량이 점프 높이를 망치지 않음.
            var v = _rb.linearVelocity;
            v.y = 0f;
            _rb.linearVelocity = v;
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.VelocityChange);
        }

        private Vector3 GetMoveDirection()
        {
            return GetCameraRelativeDirection();
        }

        private Vector3 GetCameraRelativeDirection()
        {
            // 로블록스식 상시 우클릭 홀드 느낌의 카메라 기준 이동이다.
            // W/S는 카메라 forward/back, A/D는 카메라 right/left를 수평면에 투영해서 계산한다.
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            if (_cameraTransform != null)
            {
                Vector3 camFwd = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up);
                Vector3 camRgt = Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up);
                if (camFwd.sqrMagnitude > 0.0001f && camRgt.sqrMagnitude > 0.0001f)
                    return (camFwd.normalized * _moveInput.y + camRgt.normalized * _moveInput.x).normalized;
            }

            var rig = MazeCameraRig.Instance;
            if (rig != null)
            {
                Vector3 forward = rig.GetYawForward();
                Vector3 right   = rig.GetYawRight();
                return (forward * _moveInput.y + right * _moveInput.x).normalized;
            }

            return new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
        }

        private bool CheckGround()
        {
            // 아래로만 — 캡슐 바로 위에서 수직 레이. 옆 벽을 바닥으로 착각하지 않게(벽 타기·무한점프 방지).
            float bottomY;
            if (_collider != null) bottomY = _collider.bounds.min.y;
            else                    bottomY = transform.position.y - 0.5f;

            Vector3 origin = new(
                transform.position.x,
                bottomY + 0.05f,
                transform.position.z);

            if (Physics.Raycast(origin, Vector3.down, _groundCheckDist + 0.05f,
                                _groundLayer, QueryTriggerInteraction.Ignore))
                return true;

            // 보조 SphereCast — 모서리 여유(아래로만 스윕해 벽은 안 잡음).
            float smallRadius = 0.08f * Mathf.Abs(transform.lossyScale.x);
            return Physics.SphereCast(origin, smallRadius, Vector3.down, out _,
                _groundCheckDist, _groundLayer, QueryTriggerInteraction.Ignore);
        }

        private void OnGameStateChanged(GameState state)
        {
            bool canMove = state == GameState.Playing;
            SetMovementEnabled(canMove);

            if (_lockCursor)
            {
                bool shouldLock = state == GameState.Playing || state == GameState.Countdown;
                Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible   = !shouldLock;
            }
        }

        public void SetMovementEnabled(bool enabled)
        {
            if (_moveAction == null || _jumpAction == null || _sprintAction == null) return;
            if (enabled) { _moveAction.Enable(); _jumpAction.Enable(); _sprintAction.Enable(); }
            else         { _moveAction.Disable(); _jumpAction.Disable(); _sprintAction.Disable(); }
        }

        /// <summary>스태미나 회복(예: 오브 픽업). 최대치로 클램프.</summary>
        public void RefillStamina(float amount)
        {
            if (amount <= 0f) return;
            Stamina = Mathf.Min(_maxStamina, Stamina + amount);
            if (Stamina >= _minStaminaToSprint)
                _sprintAvailableTime = 0f;
        }
    }
}
