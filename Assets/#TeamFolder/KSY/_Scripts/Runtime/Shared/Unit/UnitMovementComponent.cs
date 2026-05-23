using KSY.Utility;
using UnityEngine;

namespace KSY.Shared
{
    public class UnitMovementComponent : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody body = null;

        private Vector3 _movementInput = Vector3.zero;
        private Vector3 _moveDirection = Vector3.zero;
        private float _moveSpeed = 0f;
        private int _currentDirection = 0;

        private float _maxSpeed = 0f;
        private float _acceleration = 0f;

        public void Initialize(float maxSpeed, float acceleration)
        {
            this._maxSpeed = maxSpeed;
            this._acceleration = acceleration;
            CustomLog.Log($"Max Speed : {_maxSpeed}, Acceleration : {_acceleration}", UnityEngine.Color.purple);
        }

        private void FixedUpdate()
        {
            _moveSpeed = Mathf.Clamp(_moveSpeed + Time.fixedDeltaTime * _acceleration, 0, _maxSpeed);

            body.linearVelocity = _moveDirection * _moveSpeed;

            if (_movementInput.x != 0)
                SetDirection((int)Mathf.Sign(_movementInput.x));
        }

        private void SetDirection(int direction)
        {
            if (_currentDirection == direction)
                return;

            _currentDirection = direction;
            transform.rotation = Quaternion.Euler(0, direction > 0 ? 0 : 180, 0);
        }

        public void SetMovementInput(Vector3 input)
        {
            CustomLog.Log($"{gameObject.name}'s Move Direction : {_movementInput}", Color.blue);
            _movementInput = input.normalized;
            _moveDirection = _movementInput;
        }
    }
}

