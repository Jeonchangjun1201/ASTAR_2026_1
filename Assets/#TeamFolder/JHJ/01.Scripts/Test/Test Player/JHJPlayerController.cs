using JHJ.Scripts.EatingthegroundGame;
using JHJ.Test.TestPlayer;
using UnityEngine;

namespace JHJ.Scripts.Test.TestPlayer
{
    public enum PlayerIndex { P1, P2, P3, P4 }

    // ───────────────── [1. 서버 전송용 패킷 구조체] ─────────────────
    // 서버가 파싱하기 가장 좋게 데이터를 직렬화(Serialize)할 수 있는 바구니
    [System.Serializable]
    public struct PlayerMovementPacket
    {
        public PlayerIndex PlayerIndex; // 몇 번 플레이어인지
        public Vector3 Position;         // 현재 위치
        public Vector3 Velocity;         // 현재 속도 (이동 방향 포함)
        public Quaternion Rotation;      // 현재 회전 값
    }
    // ──────────────────────────────────────────────────────────────

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
        [SerializeField] private float groundCheckDistance = 0.2f;

        [SerializeField] private InputReader _inputReader;
        private Camera _mainCamera;

        private bool _canMove = false;

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

        private void UnlockMovement()
        {
            _canMove = true;
        }

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
            }
        }

        private void SetMove(Vector2 dir) => _moveDir = new Vector3(dir.x, 0f, dir.y);

        private void OnJump()
        {
            if (!IsGrounded()) return;
            RidCompo.linearVelocity = new Vector3(
                RidCompo.linearVelocity.x,
                jumpForce,
                RidCompo.linearVelocity.z);
        }

        private void FixedUpdate() => Move();

        private void Move()
        {
            if (_mainCamera == null) return;
            if (!_canMove)
                return;

            // 1. 카메라 기준 방향 연산
            Vector3 camForward = _mainCamera.transform.forward;
            Vector3 camRight = _mainCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 targetDir = camForward * _moveDir.z + camRight * _moveDir.x;

            // 2. 최종 적용할 속도와 회전 값 계산 (미리 변수에 담아두기)
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

            // 3. 내 화면(로컬 클라이언트)에 먼저 이동 적용
            RidCompo.linearVelocity = nextVelocity;
            transform.rotation = nextRotation;

            // 4. 🌟 계산 완료된 정보들을 매개변수로 던져서 서버 전송 메서드 호출!
            SendMovementDataToServer(_playerIndex, transform.position, nextVelocity, nextRotation);
        }

        // ───────────────── [2. 요청하신 서버 전송 전용 메서드] ─────────────────
        /// <summary>
        /// 서버나 네트워크 매니저가 받기 쉽도록, 필요한 정보들을 매개변수(Parameter)로 직접 전달받는 메서드입니다.
        /// </summary>
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

        private bool IsGrounded()
        {
            Vector3 rayStartPos = transform.position + Vector3.up * 0.1f;
            return Physics.Raycast(rayStartPos, Vector3.down, groundCheckDistance + 0.1f, whatIsGround);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 rayStartPos = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawLine(rayStartPos, rayStartPos + Vector3.down * (groundCheckDistance + 0.1f));
        }

        public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;
        public void ResetMoveSpeed() => moveSpeed = _defaultMoveSpeed;
    }
}