using JHJ.Scripts.EatingthegroundGame;
using JHJ.Test.TestPlayer;
using UnityEngine;

namespace JHJ.Scripts.Test.TestPlayer
{
    public enum PlayerIndex { P1, P2, P3, P4 }

    [System.Serializable]
    public struct PlayerMovementPacket
    {
        public PlayerIndex PlayerIndex;
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation;
    }

    [RequireComponent(typeof(Rigidbody))]
    public class JHJPlayerController : MonoBehaviour
    {
        [Header("이 캐릭터는 몇 번 플레이어인지")]
        [SerializeField] private PlayerIndex _playerIndex;
        public PlayerIndex PlayerIndex => _playerIndex;
        public Rigidbody RidCompo { get; private set; }
        private Vector3 _moveDir;

        [Header("이동 설정")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 15f;
        private float _defaultMoveSpeed;

        [Header("점프 설정")]
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private LayerMask whatIsGround;
        [SerializeField] private Vector3 groundCheckDistance;

        [SerializeField] private InputReader _inputReader;
        private Camera _mainCamera;

        private bool _canMove = true;

        //연속 점프 버그 방지용 쿨타임 변수
        private float _lastJumpTime = -999f;
        private readonly float _jumpCooldown = 0.3f;

        private void Awake()
        {
            RidCompo = GetComponent<Rigidbody>();
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            _defaultMoveSpeed = moveSpeed;
            if (JHJPaintingGameTimerManager.Instance != null)
                JHJPaintingGameTimerManager.Instance.OnGameStarted += UnlockMovement;
        }

        private void UnlockMovement() => _canMove = true;

        private void OnDestroy()
        {
            if (JHJPaintingGameTimerManager.Instance != null)
                JHJPaintingGameTimerManager.Instance.OnGameStarted -= UnlockMovement;
        }

        private void OnEnable()
        {
            switch (_playerIndex)
            {
                case PlayerIndex.P1:
                    _inputReader.P1OnMove += SetMove;
                    _inputReader.P1OnJump += OnJump;
                    break;
                case PlayerIndex.P2:
                    _inputReader.P2OnMove += SetMove;
                    _inputReader.P2OnJump += OnJump;
                    break;
                case PlayerIndex.P3:
                    _inputReader.P3OnMove += SetMove;
                    _inputReader.p3OnJump += OnJump;
                    break;
                case PlayerIndex.P4:
                    _inputReader.P4OnMove += SetMove;
                    _inputReader.p4OnJump += OnJump;
                    break;
                    
            }
        }

        private void OnDisable()
        {
            switch (_playerIndex)
            {
                case PlayerIndex.P1:
                    _inputReader.P1OnMove -= SetMove;
                    _inputReader.P1OnJump -= OnJump;
                    break;
                case PlayerIndex.P2:
                    _inputReader.P2OnMove -= SetMove;
                    _inputReader.P2OnJump -= OnJump;
                    break;
                case PlayerIndex.P3:
                    _inputReader.P3OnMove -= SetMove;
                    _inputReader.p3OnJump -= OnJump;
                    break;
                case PlayerIndex.P4:
                    _inputReader.P4OnMove -= SetMove;
                    _inputReader.p4OnJump -= OnJump;
                    break;

            }
        }

        private void SetMove(Vector2 dir) => _moveDir = new Vector3(dir.x, 0f, dir.y);

        private void OnJump()
        {
          
            if (Time.time - _lastJumpTime < _jumpCooldown) return;

            if (!IsGrounded()) return;

            //점프 직전 Y축 속도를 0으로 초기화하여 점프 높이를 항상 일정하게 보장
            RidCompo.linearVelocity = new Vector3(
                RidCompo.linearVelocity.x,
                jumpForce,
                RidCompo.linearVelocity.z);

            _lastJumpTime = Time.time; // 마지막 점프 시간 갱신
        }

        private void FixedUpdate() => Move();

        private void Move()
        {
            if (_mainCamera == null) return;
            if (!_canMove) return;

            Vector3 camForward = _mainCamera.transform.forward;
            Vector3 camRight = _mainCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 targetDir = camForward * _moveDir.z + camRight * _moveDir.x;

            Vector3 targetVelocity = targetDir * moveSpeed;
            Vector3 currentVelocity = new Vector3(RidCompo.linearVelocity.x, 0f, RidCompo.linearVelocity.z);
            Vector3 smoothedVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 10f * Time.fixedDeltaTime);

            Vector3 nextVelocity = new Vector3(smoothedVelocity.x, RidCompo.linearVelocity.y, smoothedVelocity.z);
            Quaternion nextRotation = transform.rotation;

            if (targetDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDir);
                nextRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }

            RidCompo.linearVelocity = nextVelocity;
            transform.rotation = nextRotation;

            SendMovementDataToServer(_playerIndex, transform.position, nextVelocity, nextRotation);
        }

        public void SendMovementDataToServer(PlayerIndex index, Vector3 position, Vector3 velocity, Quaternion rotation)
        {
            PlayerMovementPacket packet = new PlayerMovementPacket
            {
                PlayerIndex = index,
                Position = position,
                Velocity = velocity,
                Rotation = rotation
            };
        }

        // 얇은 선(Raycast) 대신 둥근 구(Sphere) 형태로 넓게 바닥 검사 (Fall 버그 해결)
        private bool IsGrounded()
        {
            Vector3 spherePos = transform.position + Vector3.up * 0.1f;

            // OverlapSphere를 사용해 반경(groundCheckDistance) 내의 모든 콜라이더를 찾습니다.
            Collider[] colliders = Physics.OverlapBox(spherePos, groundCheckDistance,Quaternion.identity);

            foreach (Collider col in colliders)
            {
                // 부딪힌 것들 중 '자기 자신'이 아닌 것이 하나라도 있다면 땅에 닿은 것으로 인정!
                if (col.transform.root != transform.root)
                {
                    return true;
                }
            }
            return false;
        }

        // 에디터에서 바닥 검사 범위가 눈에 보이도록 수정
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 spherePos = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawWireCube(spherePos, groundCheckDistance);
        }

        public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;
        public void ResetMoveSpeed() => moveSpeed = _defaultMoveSpeed;
    }
}