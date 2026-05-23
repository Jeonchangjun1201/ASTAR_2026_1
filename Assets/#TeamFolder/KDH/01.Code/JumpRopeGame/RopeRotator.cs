using UnityEngine;

namespace KDH
{
    public class RopeRotator : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private float rotateSpeed = 180f;
        [SerializeField] private float speedIncrement = 10f;
        [SerializeField] private float maxSpeed = 720f;

        public float CurrentSpeed => rotateSpeed;

        private void Update()
        {
            // Z축 회전 (줄넘기 앞뒤로 회전)
            transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);

            rotateSpeed += speedIncrement * Time.deltaTime;
            rotateSpeed = Mathf.Min(rotateSpeed, maxSpeed);
        }
    }
}