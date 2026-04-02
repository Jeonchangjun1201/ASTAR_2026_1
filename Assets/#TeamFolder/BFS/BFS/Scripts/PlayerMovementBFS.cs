using System;
using UnityEngine;
namespace GDH
{
    public class PlayerMovementBFS : MonoBehaviour
    {
        private CharacterController _controller;

        private Transform _targetTrm;
        private PlayerBFS _player;
        private Vector3 _movementDirection;
        private Vector3 _velocity;
        private float _verticalVelocity = 0;
        private float _moveSpeed = 0.4f;
        private float _gravity = -9.8f;

        public void Initialize(Transform trm, CharacterController controller)
        {
            _targetTrm = trm;
            _controller = controller;
            _player = GetComponentInParent<PlayerBFS>();

            _player.PlayerInput.OnMovementKeyPressed += SetMovementDirection;
        }
        private void OnDestroy()
        {
            _player.PlayerInput.OnMovementKeyPressed -= SetMovementDirection;
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
        }
    }
}

