using UnityEngine;
using System.Collections;

namespace KDH
{
    public class BlackHole : MonoBehaviour
    {
        [Header("블랙홀 설정")]
        [SerializeField] private float spawnInterval = 15f;  // 생성 간격
        [SerializeField] private float blackHoleLifetime = 5f; // 유지 시간
        [SerializeField] private float pullForce = 10f;      // 당기는 힘
        [SerializeField] private float pullRadius = 8f;      // 당기는 범위
        [SerializeField] private GameObject blackHolePrefab; // 블랙홀 프리팹

        private GameObject _currentBlackHole;

        private void Start()
        {
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);
                StartCoroutine(SpawnBlackHole());
            }
        }

        private IEnumerator SpawnBlackHole()
        {
            // 맵 중앙 근처 랜덤 위치
            Vector3 spawnPos = new Vector3(
                Random.Range(-3f, 3f),
                0.1f, // 낮게
                Random.Range(-3f, 3f)
            );

            _currentBlackHole = Instantiate(blackHolePrefab, spawnPos, Quaternion.identity);

            BlackHolePull pull = _currentBlackHole.AddComponent<BlackHolePull>();
            pull.Initialize(pullForce, pullRadius);

            // 점점 커지기
            float elapsed = 0f;
            float growDuration = 0.5f;
            while (elapsed < growDuration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(0f, 1f, elapsed / growDuration);
                _currentBlackHole.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            // 유지
            yield return new WaitForSeconds(blackHoleLifetime);

            // 점점 작아지기
            elapsed = 0f;
            while (elapsed < growDuration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 0f, elapsed / growDuration);
                _currentBlackHole.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            Destroy(_currentBlackHole);
        }
    }
}