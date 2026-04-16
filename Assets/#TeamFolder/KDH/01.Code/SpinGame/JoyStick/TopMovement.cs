using System;
using UnityEngine;

namespace KDH
{
    public class TopMovement : MonoBehaviour
    {
        public float moveSpeed = 5f;

        Rigidbody rb;
        
        
        public float spinSpeed = 10.0f;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            transform.Rotate(0, spinSpeed * Time.deltaTime, 0);
        }

        public void Move(Vector2 input)
        {
            Vector3 dir = new Vector3(input.x, 0, input.y);

            rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime );
        }
    }
}