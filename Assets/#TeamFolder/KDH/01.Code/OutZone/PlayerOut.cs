using System.Collections;
using _TeamFolder.JCJ.Script;
using TMPro;
using UnityEngine;

namespace _TeamFolder.KDH._01.Code.OutZone
{
    public class PlayerOut : MonoBehaviour
    {
        [SerializeField] private GameObject outUI;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("설정")]
        [SerializeField] private float respawnTime = 3f;
        [SerializeField] private float outRangeX = 6f; // 맵 X 경계
        [SerializeField] private float outRangeZ = 6f; // 맵 Z 경계

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Rigidbody _rb;
        private PlayerController _playerController;
        private bool _isOut = false;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            _playerController = GetComponent<PlayerController>();

            if (outUI != null) outUI.SetActive(false);
        }

        private void Update()
        {
            if (_isOut) return;

            // X, Z 좌표로 맵 경계 벗어나면 아웃
            if (Mathf.Abs(transform.position.x) > outRangeX ||
                Mathf.Abs(transform.position.z) > outRangeZ)
                TriggerOut();
        }

        public void TriggerOut()
        {
            if (_isOut) return;
            StartCoroutine(OutCoroutine());
        }

        private IEnumerator OutCoroutine()
        {
            _isOut = true;

            if (_playerController != null) _playerController.enabled = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.position = _startPosition;

            if (outUI != null) outUI.SetActive(true);

            float elapsed = respawnTime;
            while (elapsed > 0f)
            {
                elapsed -= Time.deltaTime;
                if (timerText != null)
                    timerText.text = $"부활까지 {Mathf.CeilToInt(elapsed)}초";
                yield return null;
            }

            Respawn();
        }

        private void Respawn()
        {
            transform.position = _startPosition;
            transform.rotation = _startRotation;

            if (_playerController != null) _playerController.enabled = true;
            if (outUI != null) outUI.SetActive(false);

            _isOut = false;
            Debug.Log($"{gameObject.name} 부활!");
        }
    }
}