using System;
using UnityEngine;
namespace BFS
{
    public class PlayerMovementBFS : MonoBehaviour
    {
        private CharacterController _controller;

        private Transform _targetTrm;
        private PlayerBFS _player;
        private Vector3 _movementDirection;
        private Vector3 _velocity;
        private float _verticalVelocity = 0;
        private float _moveSpeed = 0.3f;
        private float _gravity = -9.8f;
        private bool _canJump = true;

        public void Initialize(PlayerBFS player, CharacterController controller)
        {
            _player = player;
            _targetTrm = player.transform;
            _player.PlayerInput.OnMovementKeyPressed += SetMovementDirection;
            _player.PlayerInput.OnJumpKeyPressed += Jump;
            _controller = controller;
        }
        private void OnDestroy()
        {
            _player.PlayerInput.OnMovementKeyPressed -= SetMovementDirection;
            _player.PlayerInput.OnJumpKeyPressed -= Jump;
        }
        private void SetMovementDirection(Vector2 movementInput)
        {
            _movementDirection = new Vector3(movementInput.x, 0f, movementInput.y);
        }
        private void FixedUpdate()
        {
            CalculateMovement();
            ApplyGravity();
            MoveCharacter();
        }
        private void Jump()
        {
            if (!_canJump) return;
            _verticalVelocity += 1.5f;
            _canJump = false;
        }
        private void CalculateMovement()
        {
            _velocity = _movementDirection * _moveSpeed;
            
            if(_velocity.magnitude > 0f)
            {
                Quaternion q = Quaternion.LookRotation(_velocity);
                _targetTrm.rotation = Quaternion.Lerp(_targetTrm.rotation, q,
                    Time.fixedDeltaTime * 6f);
            }
        }

        private void ApplyGravity()
        {
            if (_controller.isGrounded && _verticalVelocity <= 0)
                _verticalVelocity = -0.03f;
            else
                _verticalVelocity += _gravity * Time.fixedDeltaTime;
            _velocity.y = _verticalVelocity;
        }

        private void MoveCharacter()
        {
            _controller.Move(_velocity);
            if (_controller.isGrounded && _canJump is false)
                _canJump = true;
        }
    }
}

