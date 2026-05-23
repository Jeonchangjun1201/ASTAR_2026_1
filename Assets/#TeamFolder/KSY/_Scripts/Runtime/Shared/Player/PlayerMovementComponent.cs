using KSY.Utility;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace KSY.Shared
{
    public class PlayerMovementComponent : MonoBehaviour
    {
        [SerializeField] private float gravity = -9.8f;
        [SerializeField] private CharacterController controller;

        public Transform MyTransform { get; private set; }
        public bool CanManualMove { get; set; } = true;
        public bool IsGround => controller.isGrounded;
        public Vector3 Velocity => _velocity;

        private Player _player;
        private int _runAnimationHashClip = Animator.StringToHash("RUN");
        private int _idleAnimationHashClip = Animator.StringToHash("IDLE");
        private float _speed;
        private float _rotationSpeed;
        private float _verticalVelocity;
        private Vector3 _velocity;
        private Vector3 _movementDirection;

        public void Initialize(Player player, float speed, float rotationSpeed)
        {
            this._player = player;
            this._speed = speed;
            this._rotationSpeed = rotationSpeed;
            this.MyTransform = GetComponent<Transform>();
        }

        public void SetMoveDirection(Vector3 inputDirection)
        {
            CustomLog.Log($"{inputDirection}");
            Vector3 newDirection = new Vector3(inputDirection.x, 0f, inputDirection.y);
            this._movementDirection = newDirection;

            if (_movementDirection != Vector3.zero)
                _player.RendererComponent?.PlayClip(_runAnimationHashClip, 0, 0, 0);
            else
                _player.RendererComponent?.PlayClip(_idleAnimationHashClip, 0, 0, 0);
        }

        private void FixedUpdate()
        {
            CalculateMovement();
            ApplyGravity();
            MoveCharacter();
        }

        private void CalculateMovement()
        {
            this._velocity = _movementDirection * (_speed * Time.fixedDeltaTime);

            if (_velocity.sqrMagnitude > Mathf.Epsilon)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_velocity);
                MyTransform.rotation = Quaternion.Lerp(MyTransform.rotation, targetRotation, Time.fixedDeltaTime * _rotationSpeed);
            }
        }

        private void ApplyGravity()
        {
            if (IsGround && _verticalVelocity <= 0)
            {
                _verticalVelocity = -0.3f; 
            }
            else
            {
                _verticalVelocity += gravity * Time.fixedDeltaTime;
            }
            _velocity.y = _verticalVelocity; 
        }

        private void MoveCharacter()
        {
            Physics.SyncTransforms();
            this.controller.Move(_velocity);
        }
    }
}