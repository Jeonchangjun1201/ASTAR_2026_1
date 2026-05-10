using UnityEngine;
using JHJ.Test.TestPlayer;

namespace JHJ.Scripts.Test.TestPlayer.FSM
{
    public enum PlayerIndex { P1, P2, P3, P4 }

    [RequireComponent(typeof(Rigidbody), typeof(Animator))]
    public class PlayerStateMachine : MonoBehaviour
    {
        [Header("기본 설정")]
        [SerializeField] private PlayerIndex _playerIndex;
        [SerializeField] private InputReader _inputReader;

        [Header("물리 및 이동 설정")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _jumpForce = 7f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheckPoint;

        public Rigidbody Rigidbody { get; private set; }
        public Animator Animator { get; private set; }
        public Vector2 CurrentMovementInput { get; private set; }
        public float MoveSpeed => _moveSpeed;
        public float JumpForce => _jumpForce;

        // FSM 상태 인스턴스
        private PlayerBaseState _currentState;
        public PlayerIdleState IdleState { get; private set; }
        public PlayerRunState RunState { get; private set; }
        public PlayerJumpState JumpState { get; private set; }
        public PlayerFallState FallState { get; private set; }

        // 애니메이션 파라미터 (오타 방지용 해싱)
        public readonly int AnimParamIsRunning = Animator.StringToHash("IsRunning");
        public readonly int AnimParamJump = Animator.StringToHash("Jump");
        public readonly int AnimParamIsFalling = Animator.StringToHash("IsFalling");

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Animator = GetComponent<Animator>();

         
            IdleState = new PlayerIdleState(this);
            RunState = new PlayerRunState(this);
            JumpState = new PlayerJumpState(this);
            FallState = new PlayerFallState(this);
        }

        private void OnEnable()
        {
            switch (_playerIndex)
            {
                case PlayerIndex.P1:
                    _inputReader.P1OnMove += OnMoveInput;
                    _inputReader.P1OnJump += OnJumpInput;
                    break;
                case PlayerIndex.P2:
                    _inputReader.P2OnMove += OnMoveInput;
                    _inputReader.P2OnJump += OnJumpInput;
                    break;
                    // P3, P4도 동일하게 추가(아직 안 만듦)
            }
        }

        private void OnDisable()
        {
            switch (_playerIndex)
            {
                case PlayerIndex.P1:
                    _inputReader.P1OnMove -= OnMoveInput;
                    _inputReader.P1OnJump -= OnJumpInput;
                    break;
                case PlayerIndex.P2:
                    _inputReader.P2OnMove -= OnMoveInput;
                    _inputReader.P2OnJump -= OnJumpInput;
                    break;
            }
        }

        private void Start()
        {
            ChangeState(IdleState); //시작 했을 땐 idle
        }

        private void Update()
        {
            _currentState?.UpdateState();
        }

        private void FixedUpdate()
        {
            _currentState?.FixedUpdateState();
        }

        public void ChangeState(PlayerBaseState newState)
        {
            // 나중에 서버 정보 보내줄 때 필요하면 ㄱㄱ
            _currentState?.ExitState();
            _currentState = newState;
            _currentState?.EnterState();
        }

        private void OnMoveInput(Vector2 input) => CurrentMovementInput = input;

        private void OnJumpInput()
        {
            // Idle이나 Run 상태일 때만 점프 허용
            if (_currentState == IdleState || _currentState == RunState)
            {
                ChangeState(JumpState);
            }
        }

        public bool IsGrounded()
        {
            if (_groundCheckPoint == null) return true; // 체크포인트 안 넣었을 때 에러 방지용
            return Physics.CheckSphere(_groundCheckPoint.position, 0.2f, _groundLayer);
        }

        public void ApplyMovement()
        {
            Vector3 targetVelocity = new Vector3(CurrentMovementInput.x, 0f, CurrentMovementInput.y) * _moveSpeed;
            Rigidbody.linearVelocity = new Vector3(targetVelocity.x, Rigidbody.linearVelocity.y, targetVelocity.z);

            if (targetVelocity.sqrMagnitude > 0.01f)
            {
                transform.forward = targetVelocity.normalized;
            }
        }
    }
}