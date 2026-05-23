using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace _TeamFolder.KDH._01.Code.SoccerGame.Manager
{
    public class CountDown : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float displayDuration = 1f;
        [SerializeField] private float startDuration = 0.5f;
        [SerializeField] private GameObject[] stopObj;

        public static event Action OnCountdownFinished;

        private void Start()
        {
            RestartCountdown();
        }

        public void RestartCountdown()
        {
            StopAllCoroutines();
            text.gameObject.SetActive(true);
            StartCoroutine(StartCountdown());
        }

        private IEnumerator StartCountdown()
        {
            // 카운트다운 중 오브젝트 비활성화
            foreach (var obj in stopObj)
                if (obj != null) obj.SetActive(false);

            string[] countSteps = { "5", "4", "3", "2", "1" };
            foreach (string step in countSteps)
            {
                text.text = step;
                yield return new WaitForSeconds(displayDuration);
            }

            text.text = "Start!";
            yield return new WaitForSeconds(startDuration);

            text.gameObject.SetActive(false);

            // 카운트다운 끝나면 오브젝트 활성화
            foreach (var obj in stopObj)
                if (obj != null) obj.SetActive(true);

            OnCountdownFinished?.Invoke();
        }
    }
}