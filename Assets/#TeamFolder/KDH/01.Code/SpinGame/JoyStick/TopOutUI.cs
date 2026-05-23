using System.Collections;
using TMPro;
using UnityEngine;

namespace KDH
{
    public class TopOutUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI outText;
        [SerializeField] private float displayDuration = 2f;

        private void OnEnable() => TopSpin.OnTopFallen += ShowOutUI;
        private void OnDisable() => TopSpin.OnTopFallen -= ShowOutUI;

        private void Start()
        {
            outText.gameObject.SetActive(false);
        }

        private void ShowOutUI(string fallenTop)
        {
            StartCoroutine(DisplayOut(fallenTop));
        }

        private IEnumerator DisplayOut(string fallenTop)
        {
            outText.gameObject.SetActive(true);
            outText.text = $"{fallenTop} Out!";

            yield return new WaitForSeconds(displayDuration);

            outText.gameObject.SetActive(false);
        }
    }
}