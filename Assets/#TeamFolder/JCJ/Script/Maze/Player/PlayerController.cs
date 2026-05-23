using UnityEngine;
using UnityEngine.InputSystem;
using _TeamFolder.JCJ.Battle;
using _TeamFolder.JCJ.Battle.Session;

// 입력·비주얼·게임 상태 오케스트레이션. 물리 이동은 PlayerMovementController가 담당한다.

namespace _TeamFolder.JCJ.Script
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerMovementController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("식별")]
        [Tooltip("true = 키보드 입력 받음(로컬). false = 원격/AI(가만히 있음, 잡기 결과만 받음).")]
        [SerializeField] private bool _isLocalControlled = true;

        [Header("마우스 룩")]
        [SerializeField] private bool _enableMouseLook = true;
        [SerializeField] private float _mouseSensitivity = 0.18f;
        [SerializeField] private bool _lockCursor = true;

        [Header("카메라")]
        [Tooltip("카메라 기준 이동 방향 — 비어 있으면 Movement가 Main Camera를 사용")]
        [SerializeField] private Transform _cameraTransform;

        [Header("배틀(FP)")]
        [Tooltip("배틀 로컬일 때 플레이어 몸 Yaw를 MazeCameraRig(시선)과 맞춘다.")]
        [SerializeField] private bool _battlePrototypeBodyYawDrive = true;

        [Header("비주얼(선택)")]
        [SerializeField] private bool _addTrailIfMissing;
        [Tooltip("PartyCharacters 리깅 비주얼(권장). 끄거나 프리팹 없으면 절차적 프리미티브로 폴백.")]
        [SerializeField] private bool _usePartyCharacter = true;

        public float Stamina => _movement != null ? _movement.Stamina : 0f;
        public float MaxStamina => _movement != null ? _movement.MaxStamina : 0f;
        public bool IsSprinting => _movement != null && _movement.IsSprinting;
        public float MoveSpeed => _movement != null ? _movement.MoveSpeed : 0f;
        public bool IsSpectating { get; private set; }

        private PlayerMovementController _movement;
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
        private IPlayerVisual _visual;
        private bool _lookInputEnabled = true;
        private bool _jumpEnabled = true;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovementController>();
            if (_movement == null)
                _movement = gameObject.AddComponent<PlayerMovementController>();
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _collider = GetComponent<Collider>();
            ApplyLowFrictionMaterial();

            if (_cameraTransform != null)
                _movement.SetCameraTransform(_cameraTransform);

            BuildInputActions();

            if (_addTrailIfMissing && GetComponent<TrailRenderer>() == null)
                AddDefaultTrail();

            _visual = GetComponent<IPlayerVisual>();
            if (_visual == null) _visual = AttachPreferredVisual();
            HideBasePrimitiveMesh();

            _movement.JumpPerformed += HandleJumpPerformed;
        }

        private void OnDestroy()
        {
            if (_movement != null)
                _movement.JumpPerformed -= HandleJumpPerformed;
            _inputMap?.Disable();
            _inputMap?.Dispose();
        }

        private void OnEnable()
        {
            if (_isLocalControlled) _inputMap?.Enable();
            else _inputMap?.Disable();

            if (_isLocalControlled && _enableMouseLook && _lockCursor)
                GameplayCursor.SetLocked(true);

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
                OnGameStateChanged(GameStateManager.Instance.CurrentState);
            }
        }

        private void OnDisable()
        {
            JcjFootstepAudio.Stop();
            _inputMap?.Disable();

            if (_lockCursor)
                GameplayCursor.SetLocked(false);

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (_movement == null) return;

            if (!_isLocalControlled)
            {
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
                ApplyAirborneVisuals(_movement.TickAirborne(respectJumpCooldown: false));
                UpdateRemoteVisualState();
                return;
            }

            _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            _lookInput = _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
            ApplyAirborneVisuals(_movement.TickAirborne(respectJumpCooldown: true));
            DispatchMouseLook();
            UpdateVisualState();
            MaintainGameplayCursor();
        }

        private void ApplyAirborneVisuals(PlayerAirborneVisualDelta delta)
        {
            if (delta.Landed) _visual?.OnLand();
            if (delta.StartedFall) _visual?.OnFall();
        }

        private void MaintainGameplayCursor()
        {
            if (!_isLocalControlled || !_lockCursor || !_enableMouseLook) return;
            if (SettingsPanel.IsOpen) return;
            if (BattleMatchRegistry.TryGetMatch(out _))
                GameplayCursor.SetLocked(true);
        }

        private void LateUpdate()
        {
            if (!_isLocalControlled || !_battlePrototypeBodyYawDrive) return;
            if (GetComponent<BattleWeaponManager>() == null) return;
            var battleCamera = BattleFirstPersonCamera.Instance;
            if (battleCamera == null || battleCamera.FollowTarget != transform) return;
            var rig = MazeCameraRig.Instance;
            if (rig == null) return;
            Vector3 e = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(e.x, rig.Yaw, e.z);
        }

        private void FixedUpdate()
        {
            if (!_isLocalControlled)
            {
                ClearInputState();
                return;
            }

            var gsm = GameStateManager.Instance;
            bool canMove = gsm == null || gsm.CurrentState == GameState.Playing;

            var input = new PlayerMovementInput { Move = _moveInput, SprintHeld = _sprintHeld };
            _movement.ProcessFixedStep(input, canMove);

            UpdateFootstepSfx();
        }

        private void HandleJumpPerformed()
        {
            _visual?.OnJump();
            MazeAudio.Play(MazeSfx.Jump);
        }

        public bool IsLocalControlled
        {
            get => _isLocalControlled;
            set
            {
                _isLocalControlled = value;
                ApplyLocalControlState();
                if (!_isLocalControlled) ClearInputState();
            }
        }

        private void BuildInputActions()
        {
            PlayerControllerInputModule.BuildInputActions(
                () => _isLocalControlled,
                () => _movement.BufferJump(),
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

        public void SetJumpEnabled(bool enabled)
        {
            _jumpEnabled = enabled;
            _movement.SetJumpEnabled(enabled);
            if (_jumpAction != null)
            {
                if (enabled && _moveAction != null && _moveAction.enabled) _jumpAction.Enable();
                else if (!enabled) _jumpAction.Disable();
            }
        }

        public void SetBattlePrototypeBodyYawDrive(bool enabled)
        {
            _battlePrototypeBodyYawDrive = enabled;
        }

        public void SetMovementEnabled(bool enabled)
        {
            PlayerControllerInputModule.SetMovementEnabled(enabled, _moveAction, _jumpEnabled ? _jumpAction : null, _sprintAction);
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
            bool unusedSprint = false;
            float unusedJumpBuffer = 0f;
            PlayerControllerInputModule.ClearInputState(
                ref _moveInput,
                ref _lookInput,
                ref _sprintHeld,
                ref unusedSprint,
                ref unusedJumpBuffer);
            _movement.ClearSprintState();
        }

        private void OnGameStateChanged(GameState state)
        {
            bool canMove = state == GameState.Playing;
            SetMovementEnabled(canMove);

            if (_lockCursor)
            {
                if (BattleMatchRegistry.TryGetMatch(out _))
                {
                    if (_isLocalControlled && _enableMouseLook)
                        GameplayCursor.SetLocked(true);
                    return;
                }

                bool shouldLock = state == GameState.Playing || state == GameState.Countdown;
                GameplayCursor.SetLocked(shouldLock);
            }
        }

        public void ApplyExternalSlow(float speedRatio, float duration)
        {
            _movement.ApplyExternalSlow(speedRatio, duration);
        }

        public void RefillStamina(float amount)
        {
            _movement.RefillStamina(amount);
        }

        public void SetSpectating(bool spectating)
        {
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
            PlayerControllerPresentationModule.UpdateFootstepLoop(
                _movement.IsGrounded,
                _moveInput,
                IsSprinting);
        }

        private void UpdateVisualState()
        {
            PlayerControllerPresentationModule.UpdateVisualState(_visual, _movement.IsGrounded, _moveInput, IsSprinting, _rb);
        }

        private void UpdateRemoteVisualState()
        {
            if (_visual == null || !_movement.IsGrounded || _rb == null) return;
            var planarVelocity = _rb.linearVelocity;
            planarVelocity.y = 0f;
            float speed = planarVelocity.magnitude;
            if (speed < 0.05f)
            {
                _visual.OnIdle();
                return;
            }

            float sprintMul = _movement.SprintMultiplier;
            float sprintThreshold = MoveSpeed * sprintMul * 0.8f;
            if (speed >= sprintThreshold)
            {
                _visual.OnSprint(Mathf.Clamp01(speed / (MoveSpeed * sprintMul)));
                return;
            }

            _visual.OnWalk(Mathf.Clamp01(speed / MoveSpeed));
        }
    }
}
