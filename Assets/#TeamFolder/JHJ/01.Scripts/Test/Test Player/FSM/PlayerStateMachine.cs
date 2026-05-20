using UnityEngine;
using JHJ.Test.TestPlayer;

namespace JHJ.Scripts.Test.TestPlayer.FSM
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerStateMachine : MonoBehaviour
    {
        [Header("기본 설정")]
        [SerializeField] private PlayerIndex _playerIndex;
        [SerializeField] private InputReader _inputReader;

        [Header("물리 설정")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheckPoint;

        public Rigidbody Rigidbody { get; private set; }
        public Animator Animator { get; private set; }
        public Vector2 CurrentMovementInput { get; private set; }
        public PlayerBaseState CurrentState => _currentState;

        // 애니메이션 파라미터 해시
        public readonly int isRunning = Animator.StringToHash("IsRunning");
        public readonly int isJump = Animator.StringToHash("Jump");
        public readonly int isFalling = Animator.StringToHash("IsFalling");
        //public readonly int isPunching = Animator.StringToHash("Punch");

        private PlayerBaseState _currentState;
        public PlayerIdleState IdleState { get; private set; }
        public PlayerRunState RunState { get; private set; }
        public PlayerJumpState JumpState { get; private set; }
        public PlayerFallState FallState { get; private set; }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Animator = GetComponentInChildren<Animator>();

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
                case PlayerIndex.P3:
                    _inputReader.P3OnMove += OnMoveInput;
                    _inputReader.p3OnJump += OnJumpInput;
                    break;
                case PlayerIndex.P4:
                    _inputReader.P4OnMove += OnMoveInput;
                    _inputReader.p4OnJump += OnJumpInput;
                    break;
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
                case PlayerIndex.P3:
                    _inputReader.P3OnMove -= OnMoveInput;
                    _inputReader.p3OnJump -= OnJumpInput;
                    break;
                    break;
                case PlayerIndex.P4:
                    _inputReader.P4OnMove -= OnMoveInput;
                    _inputReader.p4OnJump -= OnJumpInput;
                    break;
            }
        }

        private void Start() => ChangeState(IdleState);
        private void Update() => _currentState?.UpdateState();
        private void FixedUpdate() => _currentState?.FixedUpdateState();

        public void ChangeState(PlayerBaseState newState)
        {
            _currentState?.ExitState();
            _currentState = newState;
            _currentState?.EnterState();
        }

        private void OnMoveInput(Vector2 input) => CurrentMovementInput = input;

        private void OnJumpInput()
        {
            if (_currentState == IdleState || _currentState == RunState)
                ChangeState(JumpState);
        }

        public bool IsGrounded()
        {
            if (_groundCheckPoint == null) return true;
            return Physics.CheckSphere(_groundCheckPoint.position, 0.2f, _groundLayer);
        }
    }
}