using UnityEngine;

namespace KDH
{
    public class TopManager : MonoBehaviour
    {
        public static event System.Action<string, string> OnTopFallen; // (떨어진 팽이, 마지막 터치한 플레이어)

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Rigidbody _rb;

        private string _lastTouchPlayer = "없음";
        private bool _isFallen = false;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 팽이끼리 부딪혔을 때 마지막 터치 기록
            // 상대방 팽이에 붙어있는 TopManager의 소유자를 가져옴
            if (collision.gameObject.CompareTag("Spin1") ||
                collision.gameObject.CompareTag("Spin2") ||
                collision.gameObject.CompareTag("Spin3") ||
                collision.gameObject.CompareTag("Spin4"))
            {
                _lastTouchPlayer = collision.gameObject.name;
                Debug.Log($"{gameObject.name} 마지막 터치: {_lastTouchPlayer}");
            }
        }

        private void Update()
        {
            if (_isFallen) return;
        }
        

        // FallZone에서 호출
        public void NotifyFallen()
        {
            if (_isFallen) return;
            _isFallen = true;

            Debug.Log($"[{gameObject.name}] 마지막으로 건든 플레이어: {_lastTouchPlayer}");
            OnTopFallen?.Invoke(gameObject.name, _lastTouchPlayer);

            Respawn();
        }

        private void Respawn()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.position = _startPosition;
            transform.rotation = _startRotation;

            _isFallen = false;
            Debug.Log($"{gameObject.name} 원래 위치로 리스폰");
        }
    }
}