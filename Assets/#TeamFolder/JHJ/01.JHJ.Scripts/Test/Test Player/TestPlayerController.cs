using JHJ.Test.TestPlayer;
using UnityEngine;

namespace JHJ.Scripts.Test.TestPlayer
{
    [RequireComponent(typeof(Rigidbody))]
    public class TestPlayerController : MonoBehaviour
    {
        public Rigidbody RidCompo { get; private set; }
        private Vector3 _moveDir;

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
            if (_inputReader != null)
                _inputReader.OnMoveEvent += SetMove;
        }

        private void OnDisable()
        {
            if (_inputReader != null)
                _inputReader.OnMoveEvent -= SetMove;
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