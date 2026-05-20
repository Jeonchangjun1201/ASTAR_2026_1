using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace KDH.Gimic
{
    public class PlayerBlind : MonoBehaviour
    {
        [SerializeField] private Image blindImage;

        public void StartBlind(float duration)
        {
            StartCoroutine(BlindCoroutine(duration));
        }

        private IEnumerator BlindCoroutine(float duration)
        {
            // 빨간 화면 켜기
            blindImage.gameObject.SetActive(true);

            float elapsed = 0f;
            Color color = new Color(1f, 0f, 0f, 0.7f); 
            blindImage.color = color;

            // 서서히 사라지기
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.7f, 0f, elapsed / duration);
                blindImage.color = new Color(1f, 0f, 0f, alpha);
                yield return null;
            }

            blindImage.gameObject.SetActive(false);
        }
    }
}