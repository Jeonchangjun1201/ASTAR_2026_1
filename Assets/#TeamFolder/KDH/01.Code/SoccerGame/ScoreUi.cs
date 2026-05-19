using System.Collections;
using TMPro;
using UnityEngine;

namespace KDH
{
    public class ScoreUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private float scoreShowTime = 2f;

        private void OnEnable()  => Ball.OnGoalScored += ShowScore;
        private void OnDisable() => Ball.OnGoalScored -= ShowScore;

        private void Start()
        {
            scoreText.gameObject.SetActive(false);
        }

        private void ShowScore(GameObject scorer, string goalOwnerName)
        {
            string scorerName = scorer != null ? scorer.name : "알 수 없음";
            StartCoroutine(DisplayScore(scorerName, goalOwnerName));
        }

        private IEnumerator DisplayScore(string scorerName, string goalOwnerName)
        {
            scoreText.gameObject.SetActive(true);
            scoreText.text = $"{scorerName} + 1";

            yield return new WaitForSeconds(scoreShowTime);

            scoreText.gameObject.SetActive(false);
        }
    }
}