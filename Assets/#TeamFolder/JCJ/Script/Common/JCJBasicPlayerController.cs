using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 단순 이동과 점프를 담당하는 범용 플레이어 컨트롤러.

namespace _TeamFolder.JCJ.Script
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class JCJBasicPlayerController : MonoBehaviour
    {
        [SerializeField] private bool _isLocalControlled = true;
        [SerializeField] private bool _useAnimatedVisual = true;
        [SerializeField] private bool _fallbackToProceduralVisual = true;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _jumpForce = 6f;
        [SerializeField] private float _rotationSpeed = 14f;
        [SerializeField] private float _groundCheckDistance = 0.25f;
        [SerializeField] private float _coyoteTime = 0.12f;
        [SerializeField] private float _jumpBufferTime = 0.15f;
        [SerializeField] private float _fallVelocityThreshold = -0.5f;
        [SerializeField] private LayerMask _groundMask;

        public bool IsGrounded { get; private set; }
        public bool IsFalling { get; private set; }
        public float VerticalSpeed => _rigidbody != null ? _rigidbody.linearVelocity.y : 0f;
        public Vector2 MoveInput => _moveInput;
        public event Action StartedFalling;
        public event Action Landed;

        private Rigidbody _rigidbody;
        private CapsuleCollider _capsuleCollider;
        private InputActionMap _inputMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private JCJBasicPlayerBindingService _bindingService;
        private IPlayerVisual _visual;
        private Vector2 _moveInput;
        private float _lastGroundedTime = -999f;
        private float _jumpBufferedUntil = -999f;
        private bool _wasGrounded;

        public InputActionMap GetInputMap()
        {
            return _inputMap;
        }

        public bool IsLocalControlled
        {
            get => _isLocalControlled;
            set
            {
                _isLocalControlled = value;
                ApplyLocalControlState();
                if (!_isLocalControlled)
                {
                    _moveInput = Vector2.zero;
                    _jumpBufferedUntil = -999f;
                    _visual?.OnIdle();
                }
            }
        }

        public string GetBindingPath(JCJBasicPlayerBindingKey key)
        {
            return _bindingService != null
                ? _bindingService.GetBindingPath(key)
                : JCJBasicPlayerInputActions.GetDefaultPath(key);
        }

        public void SetBindingPath(JCJBasicPlayerBindingKey key, string path)
        {
            _bindingService ??= JCJBasicPlayerBindingService.EnsureInstance();
            _bindingService.SetBindingPath(key, path);
        }

        public void ResetBindings()
        {
            _bindingService ??= JCJBasicPlayerBindingService.EnsureInstance();
            _bindingService.ResetToDefaults();
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _capsuleCollider = GetComponent<CapsuleCollider>();

            _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (_groundMask.value == 0)
            {
                _groundMask = Physics.DefaultRaycastLayers;
            }

            _inputMap = JCJBasicPlayerInputActions.CreateMap();
            _moveAction = JCJBasicPlayerInputActions.Find(_inputMap, JCJBasicPlayerInputActions.ActionMove);
            _jumpAction = JCJBasicPlayerInputActions.Find(_inputMap, JCJBasicPlayerInputActions.ActionJump);

            _visual = GetComponent<IPlayerVisual>();
            if (_visual == null && _useAnimatedVisual)
            {
                _visual = gameObject.AddComponent<PartyCharacterVisual>();
            }
            else if (_visual == null && _fallbackToProceduralVisual)
            {
                _visual = gameObject.AddComponent<PlayerVisualController>();
            }

            _bindingService = JCJBasicPlayerBindingService.EnsureInstance();
            if (_bindingService != null)
            {
                _bindingService.OnChanged += HandleBindingsChanged;
                HandleBindingsChanged(_bindingService.Data);
            }
        }

        private void OnEnable()
        {
            ApplyLocalControlState();
        }

        private void OnDisable()
        {
            _inputMap?.Disable();
        }

        private void OnDestroy()
        {
            if (_bindingService != null)
            {
                _bindingService.OnChanged -= HandleBindingsChanged;
            }

            _inputMap?.Disable();
            _inputMap?.Dispose();
        }

        private void Update()
        {
            if (!_isLocalControlled)
            {
                _moveInput = Vector2.zero;
                UpdateGroundState();
                return;
            }

            _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

            if (_jumpAction != null && _jumpAction.WasPressedThisFrame())
            {
                _jumpBufferedUntil = Time.time + _jumpBufferTime;
            }

            UpdateGroundState();
            UpdateVisualState();
        }

        private void FixedUpdate()
        {
            if (!_isLocalControlled || _rigidbody == null)
            {
                return;
            }

            ApplyMovement();

            var canUseBufferedJump = Time.time <= _jumpBufferedUntil;
            var canUseCoyoteJump = (Time.time - _lastGroundedTime) <= _coyoteTime;

            if (canUseBufferedJump && canUseCoyoteJump)
            {
                ApplyJump();
                _visual?.OnJump();
                _jumpBufferedUntil = -999f;
                _lastGroundedTime = -999f;
                IsGrounded = false;
                IsFalling = false;
            }
        }

        private void HandleBindingsChanged(JCJBasicPlayerBindingsData data)
        {
            JCJBasicPlayerInputActions.ApplyBindings(_inputMap, data);
        }

        private void UpdateVisualState()
        {
            if (_visual == null)
            {
                return;
            }

            if (!IsGrounded)
            {
                return;
            }

            if (_moveInput.sqrMagnitude < 0.0001f)
            {
                _visual.OnIdle();
                return;
            }

            _visual.OnWalk(Mathf.Clamp01(_moveInput.magnitude));
        }

        private void ApplyLocalControlState()
        {
            if (_inputMap == null)
            {
                return;
            }

            if (_isLocalControlled)
            {
                _inputMap.Enable();
            }
            else
            {
                _inputMap.Disable();
            }
        }

        private void UpdateGroundState()
        {
            _wasGrounded = IsGrounded;
            IsGrounded = CheckGround();

            if (IsGrounded)
            {
                _lastGroundedTime = Time.time;
            }

            var isDescending = _rigidbody != null && _rigidbody.linearVelocity.y < _fallVelocityThreshold;
            var shouldFall = !IsGrounded && isDescending;

            if (!_wasGrounded && IsGrounded)
            {
                IsFalling = false;
                _visual?.OnLand();
                Landed?.Invoke();
            }
            else if (!IsFalling && shouldFall)
            {
                IsFalling = true;
                _visual?.OnFall();
                StartedFalling?.Invoke();
            }
            else if (IsGrounded)
            {
                IsFalling = false;
            }
        }

        private void ApplyMovement()
        {
            var direction = new Vector3(_moveInput.x, 0f, _moveInput.y);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            var velocity = _rigidbody.linearVelocity;
            velocity.x = direction.x * _moveSpeed;
            velocity.z = direction.z * _moveSpeed;
            _rigidbody.linearVelocity = velocity;

            if (direction.sqrMagnitude > 0.0001f)
            {
                var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * _rotationSpeed);
            }
        }

        private void ApplyJump()
        {
            var velocity = _rigidbody.linearVelocity;
            velocity.y = 0f;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.VelocityChange);
        }

        private bool CheckGround()
        {
            if (_capsuleCollider == null)
            {
                return false;
            }

            var bounds = _capsuleCollider.bounds;
            var origin = new Vector3(bounds.center.x, bounds.min.y + 0.05f, bounds.center.z);
            var radius = Mathf.Max(0.05f, bounds.extents.x * 0.7f);
            var distance = _groundCheckDistance + 0.05f;

            return Physics.SphereCast(
                origin,
                radius,
                Vector3.down,
                out _,
                distance,
                _groundMask,
                QueryTriggerInteraction.Ignore);
        }
    }
}
