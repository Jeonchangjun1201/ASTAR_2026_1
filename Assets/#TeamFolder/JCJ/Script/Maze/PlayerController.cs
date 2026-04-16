using UnityEngine;
using UnityEngine.InputSystem;

namespace _TeamFolder.JCJ.Script
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed      = 6f;
        [SerializeField] private float _jumpForce      = 6f;
        [SerializeField] private float _rotationSpeed  = 15f;

        [Header("Ground Check")]
        [SerializeField] private float     _groundCheckDist = 0.25f;
        [SerializeField] private LayerMask _groundLayer;

        [Header("Camera")]
        [Tooltip("카메라 기준 이동 방향 계산용 — Main Camera Transform 연결")]
        [SerializeField] private Transform _cameraTransform;

        private Rigidbody  _rb;
        private InputAction _moveAction;
        private InputAction _jumpAction;

        private Vector2 _moveInput;
        private bool    _jumpPressed;
        private bool    _isGrounded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true; // 물리 회전 방지 (직접 Slerp로 제어)

            BuildInputActions();

            // 카메라 미지정 시 Main Camera 자동 할당
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        private void BuildInputActions()
        {
            // WASD + 게임패드 왼쪽 스틱 (Input Action Asset 없이 코드로 생성)
            _moveAction = new InputAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up",    "<Keyboard>/w")
                .With("Down",  "<Keyboard>/s")
                .With("Left",  "<Keyboard>/a")
                .With("Right", "<Keyboard>/d")
                .With("Up",    "<Gamepad>/leftStick/up")
                .With("Down",  "<Gamepad>/leftStick/down")
                .With("Left",  "<Gamepad>/leftStick/left")
                .With("Right", "<Gamepad>/leftStick/right");

            // Space + 게임패드 남쪽 버튼 (A/×)
            _jumpAction = new InputAction("Jump", InputActionType.Button);
            _jumpAction.AddBinding("<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");
            _jumpAction.performed += _ => _jumpPressed = true;
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _jumpAction.Enable();

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            _moveAction.Disable();
            _jumpAction.Disable();

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
        private void Update()
        {
            _moveInput  = _moveAction.ReadValue<Vector2>();
            _isGrounded = CheckGround();
        }

        private void FixedUpdate()
        {
            // Playing 상태에서만 이동
            if (GameStateManager.Instance != null &&
                GameStateManager.Instance.CurrentState != GameState.Playing) return;

            ApplyMovement();

            if (_jumpPressed && _isGrounded) ApplyJump();
            _jumpPressed = false;
        }
        private void ApplyMovement()
        {
            Vector3 direction = GetCameraRelativeDirection();

            Vector3 velocity = direction * _moveSpeed;
            velocity.y = _rb.linearVelocity.y;
            _rb.linearVelocity = velocity;

            // 이동 방향으로 부드럽게 회전
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    Time.fixedDeltaTime * _rotationSpeed);
            }
        }

        private void ApplyJump()
        {
            // 점프 전 수직 속도 초기화 (이중 점프 방지)
            _rb.linearVelocity = new Vector3(
                _rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }

        // 카메라 기준으로 수평 이동 방향 계산
        private Vector3 GetCameraRelativeDirection()
        {
            if (_cameraTransform == null)
                return new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

            Vector3 forward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
            Vector3 right   = Vector3.ProjectOnPlane(_cameraTransform.right,   Vector3.up).normalized;
            return (forward * _moveInput.y + right * _moveInput.x).normalized;
        }

        private bool CheckGround()
        {
            return Physics.Raycast(
                transform.position + Vector3.up * 0.05f,
                Vector3.down,
                _groundCheckDist + 0.05f,
                _groundLayer);
        }

        private void OnGameStateChanged(GameState state)
        {
            bool canMove = state == GameState.Playing;
            if (canMove) { _moveAction.Enable(); _jumpAction.Enable(); }
            else         { _moveAction.Disable(); _jumpAction.Disable(); }
        }

        // 외부에서 강제 제어 (연출 등)
        public void SetMovementEnabled(bool enabled)
        {
            if (enabled) { _moveAction.Enable(); _jumpAction.Enable(); }
            else         { _moveAction.Disable(); _jumpAction.Disable(); }
        }
    }
}
