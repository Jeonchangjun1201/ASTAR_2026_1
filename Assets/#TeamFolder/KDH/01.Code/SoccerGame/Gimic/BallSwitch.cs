using UnityEngine;
using System.Collections;

namespace KDH
{
    public class BallSwitcher : MonoBehaviour
    {
        [Header("공 프리팹 3개")]
        [SerializeField] private GameObject normalBallPrefab;
        [SerializeField] private GameObject redBallPrefab;
        [SerializeField] private GameObject iceBallPrefab;

        [Header("설정")]
        [SerializeField] private float switchInterval = 10f;
        [SerializeField] private Transform spawnPoint; // 맵 중앙 빈 오브젝트

        private GameObject _currentBall;

        private void Start()
        {
            StartCoroutine(SwitchLoop());
        }

        private IEnumerator SwitchLoop()
        {
            SpawnRandomBall();

            while (true)
            {
                yield return new WaitForSeconds(switchInterval);

                // 현재 공 제거 후 새 공 스폰
                if (_currentBall != null)
                    Destroy(_currentBall);

                SpawnRandomBall();
            }
        }

        private void SpawnRandomBall()
        {
            GameObject[] prefabs = { normalBallPrefab, redBallPrefab, iceBallPrefab };
            string[] names = { "일반 공", "빨간 공", "파란 공" };

            int index = Random.Range(0, prefabs.Length);
            _currentBall = Instantiate(prefabs[index], spawnPoint.position, Quaternion.identity);

            Debug.Log($"공 변경 → {names[index]}");
        }
    }
}