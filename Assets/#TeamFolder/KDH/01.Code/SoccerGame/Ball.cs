using System.Collections;
using UnityEngine;

namespace KDH
{
    public class Ball : MonoBehaviour
    {
        [SerializeField] private float ballSpeed = 5f;

        private Rigidbody _rb;
        private Vector3 _lastVelocity;
        private Vector3 _startPosition;

        public GameObject LastTouchPlayer { get; private set; }

        public static event System.Action<GameObject, string> OnGoalScored;

        private bool _isReady = false;
        public bool IsReady => _isReady;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _startPosition = transform.position;
            StartCoroutine(ReadyDelay());
            LaunchBall();
        }

        private IEnumerator ReadyDelay()
        {
            yield return new WaitForSeconds(0.8f);
            _isReady = true;
            Debug.Log("공 준비 완료");
        }

        private void FixedUpdate()
        {
            if (_rb.linearVelocity.magnitude > 0.1f)
                _lastVelocity = _rb.linearVelocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isReady) return;

            if (collision.gameObject.CompareTag("Player1") ||
                collision.gameObject.CompareTag("Player2") ||
                collision.gameObject.CompareTag("Player3") ||
                collision.gameObject.CompareTag("Player4"))
            {
                LastTouchPlayer = collision.gameObject;
                Debug.Log($"마지막 터치: {LastTouchPlayer.name}");
            }

            if (collision.gameObject.CompareTag("Wall"))
                ReflectBall(collision.contacts[0].normal);
        }

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
            _isReady = false;
            StartCoroutine(ReadyDelay());
            LastTouchPlayer = null;
            LaunchBall();
        }

        // 서버에서 위치/방향 받아서 적용
        public void ApplyTransform(Vector3 position, Vector3 velocity)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.position = position;
            _rb.linearVelocity = velocity;
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