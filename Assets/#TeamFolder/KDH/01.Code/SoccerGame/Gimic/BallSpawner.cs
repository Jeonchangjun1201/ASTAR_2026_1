using UnityEngine;
using System.Collections;
using _TeamFolder.KDH._01.Code.SoccerGame.Manager;

namespace KDH
{
    public class BallSpawner : MonoBehaviour
    {
        [Header("공 프리팹 3개")]
        [SerializeField] private GameObject normalBallPrefab;
        [SerializeField] private GameObject redBallPrefab;
        [SerializeField] private GameObject iceBallPrefab;

        [Header("설정")]
        [SerializeField] private float respawnDelay = 2f; // 리스폰 딜레이

        private GameObject _currentBall;
        private int _lastIndex = -1;

        private void OnEnable() => CountDown.OnCountdownFinished += StartGame;
        private void OnDisable() => CountDown.OnCountdownFinished -= StartGame;

        private void StartGame()
        {
            SpawnRandomBall();
        }

        public void OnGoalScored()
        {
            if (_currentBall != null)
                Destroy(_currentBall);

            StartCoroutine(RespawnDelay());
        }

        private IEnumerator RespawnDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            SpawnRandomBall();
        }

        private void SpawnRandomBall()
        {
            GameObject[] prefabs = { normalBallPrefab, redBallPrefab, iceBallPrefab };
            string[] names = { "일반 공", "불 공", "얼음 공" };

            int index;
            do {
                index = Random.Range(0, prefabs.Length);
            } while (index == _lastIndex);

            _lastIndex = index;
            _currentBall = Instantiate(prefabs[index], transform.position, Quaternion.identity);

            Debug.Log($"공 스폰 → {names[index]}");
        }
    }
}