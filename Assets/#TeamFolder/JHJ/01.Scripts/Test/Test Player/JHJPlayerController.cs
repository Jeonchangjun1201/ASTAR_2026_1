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
        [SerializeField] private float moveSpeed;
        private float _defaultMoveSpeed;

        [Header("점프 설정")]
        [SerializeField] private float jumpForce = 7f;

        [SerializeField] private InputReader _inputReader;

        private void Awake() => RidCompo = GetComponent<Rigidbody>();
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
            Vector3 targetVelocity = _moveDir * moveSpeed;
            RidCompo.linearVelocity = new Vector3(
                targetVelocity.x, RidCompo.linearVelocity.y, targetVelocity.z);

            if (_moveDir.sqrMagnitude > 0.01f)
                transform.forward = _moveDir.normalized;
        }

        public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;
        public void ResetMoveSpeed() => moveSpeed = _defaultMoveSpeed;
    }
}