using UnityEngine;

namespace KDH
{
    public class RopeRotator : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private float rotateSpeed = 180f;    // 초당 회전 각도
        [SerializeField] private float speedIncrement = 10f;  // 점점 빨라지는 속도
        [SerializeField] private float maxSpeed = 720f;       // 최대 속도

        public float CurrentSpeed => rotateSpeed;

        private void Update()
        {
            // 줄 회전
            transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);

            // 점점 빨라지기
            rotateSpeed += speedIncrement * Time.deltaTime;
            rotateSpeed = Mathf.Min(rotateSpeed, maxSpeed);
        }
    }
}