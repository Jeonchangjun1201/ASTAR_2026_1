using UnityEngine;

namespace KDH
{
    public class Ball : MonoBehaviour
    {
        [SerializeField] private float ballSpeed = 5f;

        private Rigidbody _rb;
        private Vector3 _lastVelocity;
        private Vector3 _startPosition;

        // string 대신 GameObject 참조로 변경 (더 효율적)
        public GameObject LastTouchPlayer { get; private set; }

        // 골 이벤트: (scorer, goalOwner) - 누가 넣었고 어느 골대인지
        public static event System.Action<GameObject, string> OnGoalScored;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _startPosition = transform.position;
            LaunchBall();
        }

        private void FixedUpdate()
        {
            if (_rb.linearVelocity.magnitude > 0.1f)
                _lastVelocity = _rb.linearVelocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                LastTouchPlayer = collision.gameObject;
                Debug.Log($"마지막 터치: {LastTouchPlayer.name}");
            }

            if (collision.gameObject.CompareTag("Wall"))
                ReflectBall(collision.contacts[0].normal);
        }

        // GoalZone에서 호출
        public void NotifyGoal(string goalOwnerName)
        {
            OnGoalScored?.Invoke(LastTouchPlayer, goalOwnerName);
            Debug.Log($"골! {LastTouchPlayer?.name ?? "알 수 없음"} → {goalOwnerName} 골대");
        }

        public void ResetBall()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.position = _startPosition;
            LastTouchPlayer = null;
            LaunchBall();
        }

        private void ReflectBall(Vector3 normal)
        {
            Vector3 reflect = Vector3.Reflect(_lastVelocity.normalized, normal);
            reflect.y = 0;
            _rb.linearVelocity = reflect.normalized * ballSpeed;
            transform.position += reflect.normalized * 0.1f;
        }

        private void LaunchBall()
        {
            Vector3 dir = new Vector3(1f, 0f, 0.5f).normalized;
            _rb.linearVelocity = dir * ballSpeed;
        }
    }
}