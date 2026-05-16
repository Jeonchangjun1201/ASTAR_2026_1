using JHJ.Test.TestPlayer;
using UnityEngine;

namespace JHJ.Scripts.Test.TestPlayer
{
    public enum PlayerIndex { P1, P2, P3, P4 }

    [RequireComponent(typeof(Rigidbody))]
    public class JHJPlayerController : MonoBehaviour
    {
        [Header("이 캐릭터는 몇 번 플레이어인지")]
        [SerializeField] private PlayerIndex _playerIndex;
        public Rigidbody RidCompo { get; private set; }
        private Vector3 _moveDir;

        [Header("이동 설정")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 15f; // 방향 바꿀 때 휙 도는 속도
        private float _defaultMoveSpeed;

        [Header("점프 설정")]
        [SerializeField] private float jumpForce = 7f;

        [SerializeField] private InputReader _inputReader;

        private Camera _mainCamera; // 카메라 기준 이동을 위해 필요함

        private void Awake()
        {
            RidCompo = GetComponent<Rigidbody>();
            _mainCamera = Camera.main; // 씬의 메인 카메라 캐싱
        }

        private void Start() => _defaultMoveSpeed = moveSpeed;

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
            RidCompo.linearVelocity = new Vector3(
                RidCompo.linearVelocity.x,
                jumpForce,
                RidCompo.linearVelocity.z);
        }

        private void FixedUpdate() => Move();

        private void Move()
        {
            if (_mainCamera == null) return;

            // 1. 카메라가 바라보는 '앞'과 '오른쪽' 벡터를 가져옴 (Y축 기울어짐은 무시)
            Vector3 camForward = _mainCamera.transform.forward;
            Vector3 camRight = _mainCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // 2. 입력값을 카메라 방향에 맞춰 회전 변환
            // W(직진)를 누르면 무조건 카메라가 보는 앞쪽으로 가게 됨
            Vector3 targetDir = camForward * _moveDir.z + camRight * _moveDir.x;

            // 3. 속도 적용 (부드러운 가속)
            Vector3 targetVelocity = targetDir * moveSpeed;
            Vector3 currentVelocity = new Vector3(RidCompo.linearVelocity.x, 0f, RidCompo.linearVelocity.z);
            Vector3 smoothedVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 10f * Time.fixedDeltaTime);

            RidCompo.linearVelocity = new Vector3(smoothedVelocity.x, RidCompo.linearVelocity.y, smoothedVelocity.z);

            // 4. 캐릭터 몸통을 이동하는 방향으로 부드럽게 회전
            if (targetDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;
        public void ResetMoveSpeed() => moveSpeed = _defaultMoveSpeed;
    }
}