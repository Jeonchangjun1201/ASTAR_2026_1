using UnityEngine;

namespace KDH
{
    
        public class Ball : MonoBehaviour
        {
            [SerializeField] private float ballSpeed = 5f;

            private Rigidbody _rb;
            private Vector3 lastVelocity;
            private Vector3 startPosition;

            // 기본 플레이어 이름 player1
            private string lastTouchPlayer = "Player 1"; 

            public string LastTouchPlayer => lastTouchPlayer; //마지막에 건든 사람을 바꾸기 위해 적음

            private void Start()
            {
                _rb = GetComponent<Rigidbody>();
                startPosition = transform.position; 
                LaunchBall();
            }

            private void FixedUpdate()
            {
                if (_rb.linearVelocity.magnitude > 0.1f) // 벡터 길이 구하기
                {
                    lastVelocity = _rb.linearVelocity;
                }
            }

            private void OnCollisionEnter(Collision collision)
            {
                // 막지막 터치 한사람 알려주는거
                if (collision.gameObject.CompareTag("Player"))
                {
                    lastTouchPlayer = collision.gameObject.name;
                    Debug.Log($"막터 : {lastTouchPlayer}");
                }

                // 대충 벽에 닿았을때 반사 해주는 코드
                if (collision.gameObject.CompareTag("Wall"))
                {
                    Vector3 normal = collision.contacts[0].normal;
                    Vector3 reflect = Vector3.Reflect(lastVelocity.normalized, normal);
                    reflect.y = 0; 

                    _rb.linearVelocity = reflect.normalized * ballSpeed;
                    transform.position += reflect.normalized * 0.1f;
                }
            }

            public void ResetBall()
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                transform.position = startPosition;

                // 공이 리셋되어도 마지막 터치 플레이어를 초기화하지 않습니다.
                // 직전에 넣은 사람이 계속 유지되거나 다음 터치자가 생길 때까지 기억합니다.

                Debug.Log("공이 원점으로 리셋되었습니다.");
                LaunchBall(); 
            }

            private void LaunchBall()
            {
                _rb.linearVelocity = new Vector3(ballSpeed, 0f, ballSpeed * 0.5f).normalized * ballSpeed; //공 속도
            }
        }
}