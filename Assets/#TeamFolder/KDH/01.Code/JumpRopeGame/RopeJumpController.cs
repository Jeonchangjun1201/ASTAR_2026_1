using JHJ.Scripts.Test.TestPlayer;
using UnityEngine;
using JHJ.Test.TestPlayer;

namespace _TeamFolder.KDH._01.Code.JumpRopeGame
{
    public class RopeJumpController : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private PlayerIndex playerIndex;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private Vector3 groundCheckSize;

        private Rigidbody _rb;
        private float _lastJumpTime = -999f;
        private readonly float _jumpCooldown = 0.3f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            switch (playerIndex)
            {
                case PlayerIndex.P1: inputReader.P1OnJump += OnJump; break;
                case PlayerIndex.P2: inputReader.P2OnJump += OnJump; break;
                case PlayerIndex.P3: inputReader.p3OnJump += OnJump; break;
                case PlayerIndex.P4: inputReader.p4OnJump += OnJump; break;
            }
        }

        private void OnDisable()
        {
            switch (playerIndex)
            {
                case PlayerIndex.P1: inputReader.P1OnJump -= OnJump; break;
                case PlayerIndex.P2: inputReader.P2OnJump -= OnJump; break;
                case PlayerIndex.P3: inputReader.p3OnJump -= OnJump; break;
                case PlayerIndex.P4: inputReader.p4OnJump -= OnJump; break;
            }
        }

        private void OnJump()
        {
            if (Time.time - _lastJumpTime < _jumpCooldown) return;
            if (!IsGrounded()) return;

            _rb.linearVelocity = new Vector3(
                _rb.linearVelocity.x,
                jumpForce,
                _rb.linearVelocity.z);

            _lastJumpTime = Time.time;
        }

        private bool IsGrounded()
        {
            Vector3 spherePos = transform.position + Vector3.up * 0.1f;
            Collider[] colliders = Physics.OverlapBox(spherePos, groundCheckSize, Quaternion.identity);

            foreach (Collider col in colliders)
                if (col.transform.root != transform.root)
                    return true;

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.1f, groundCheckSize);
        }
    }
}