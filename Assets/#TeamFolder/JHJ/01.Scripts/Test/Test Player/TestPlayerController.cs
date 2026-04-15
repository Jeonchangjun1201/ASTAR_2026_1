using JHJ.Test.TestPlayer;
using UnityEngine;

namespace JHJ.Scripts.Test.TestPlayer
{
    // 💡 1. 4명의 플레이어를 구분할 '이름표(Enum)'를 만듦
    public enum PlayerIndex
    {
        P1, P2, P3, P4
    }

    [RequireComponent(typeof(Rigidbody))]
    public class TestPlayerController : MonoBehaviour
    {
        [Header("이 캐릭터는 몇 번 플레이어입니까?")]
        [SerializeField] private PlayerIndex _playerIndex; 

        public Rigidbody RidCompo { get; private set; }
        private Vector3 _moveDir;

        [Header("이동 설정")]
        [SerializeField] private float moveSpeed;
        private float _originalMoveSpeed;

        [SerializeField] private InputReader _inputReader;

        private void Awake()
        {
            RidCompo = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _originalMoveSpeed = moveSpeed;
        }

        private void OnEnable()
        {
            switch (_playerIndex)
            {
                case PlayerIndex.P1: _inputReader.P1OnMove += SetMove; break;
                case PlayerIndex.P2: _inputReader.P2OnMove += SetMove; break;
                case PlayerIndex.P3: _inputReader.P3OnMove += SetMove; break;
                case PlayerIndex.P4: _inputReader.P4OnMove += SetMove; break;
            }
        }

        private void OnDisable()
        {
            switch (_playerIndex)
            {
                case PlayerIndex.P1: _inputReader.P1OnMove -= SetMove; break;
                case PlayerIndex.P2: _inputReader.P2OnMove -= SetMove; break;
                case PlayerIndex.P3: _inputReader.P3OnMove -= SetMove; break;
                case PlayerIndex.P4: _inputReader.P4OnMove -= SetMove; break;
            }
        }

        private void SetMove(Vector2 dir)
        {
            _moveDir = new Vector3(dir.x, 0f, dir.y);
        }
        private void FixedUpdate()
        {
            Move();
        }

        private void Move()
        {
            Vector3 targetVelocity = _moveDir * moveSpeed;
            RidCompo.linearVelocity = new Vector3(targetVelocity.x, RidCompo.linearVelocity.y, targetVelocity.z);
        }

        public void SetMoveSpeed(float newSpeed)
        {
            moveSpeed = newSpeed;
        }

        public void ResetMoveSpeed()
        {
            moveSpeed = _originalMoveSpeed;
        }
    }
}