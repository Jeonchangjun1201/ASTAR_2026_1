using UnityEngine;

namespace KSY.Shared
{
    public class UnitMovementComponent : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody body = null;

        private Vector3 movementInput = Vector3.zero;
        private Vector3 moveDirection = Vector3.zero;
        private float moveSpeed = 0f;
        private int currentDirection = 0;

        private float maxSpeed = 0f;
        private float acceleration = 0f;

        public void Initialize(float maxSpeed, float acceleration)
        {
            this.maxSpeed = maxSpeed;
            this.acceleration = acceleration;
        }

        private void FixedUpdate()
        {
            float acceleration = this.acceleration * (movementInput == Vector3.zero ? -1 : 1);
            moveSpeed = Mathf.Clamp(moveSpeed + Time.fixedDeltaTime * acceleration, 0, maxSpeed);

            body.linearVelocity = moveDirection * moveSpeed;

            if (movementInput.x != 0)
                SetDirection((int)Mathf.Sign(movementInput.x));
        }

        private void SetDirection(int direction)
        {
            if (currentDirection == direction)
                return;

            currentDirection = direction;
            transform.rotation = Quaternion.Euler(0, direction > 0 ? 0 : 180, 0);
        }

        public void SetMovementInput(Vector3 input)
        {
            movementInput = input.normalized;
            if (movementInput != Vector3.zero)
                moveDirection = movementInput;
        }
    }
}

