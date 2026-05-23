using UnityEngine;
using System.Collections;

namespace KDH
{
    public class MapTilt : MonoBehaviour
    {
        [Header("기울기 설정")]
        [SerializeField] private float tiltInterval = 10f;  // 기울기 변경 간격
        [SerializeField] private float maxTiltAngle = 15f;  // 최대 기울기 각도
        [SerializeField] private float tiltDuration = 2f;   // 기울어지는 시간

        private Quaternion _originalRotation;
        private Quaternion _targetRotation;

        private void Start()
        {
            _originalRotation = transform.rotation;
            StartCoroutine(TiltLoop());
        }

        private IEnumerator TiltLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(tiltInterval);

                // 랜덤 방향으로 기울기
                float randomX = Random.Range(-maxTiltAngle, maxTiltAngle);
                float randomZ = Random.Range(-maxTiltAngle, maxTiltAngle);
                _targetRotation = Quaternion.Euler(randomX, 0f, randomZ);

                // 부드럽게 기울어지기
                yield return StartCoroutine(TiltTo(_targetRotation));

                // 유지
                yield return new WaitForSeconds(tiltInterval);

                // 원래대로 돌아오기
                yield return StartCoroutine(TiltTo(_originalRotation));
            }
        }

        private IEnumerator TiltTo(Quaternion target)
        {
            Quaternion start = transform.rotation;
            float elapsed = 0f;

            while (elapsed < tiltDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / tiltDuration);
                transform.rotation = Quaternion.Slerp(start, target, t);
                yield return null;
            }

            transform.rotation = target;
        }
    }
}