using UnityEngine;
using TMPro;
using System.Collections;
using System.Text; 

namespace KSY.Shared.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class UILoadingText : MonoBehaviour
    {
        [SerializeField] private string baseText = "Loading";
        [SerializeField] private float dotChangeInterval = 0.5f;
        [SerializeField] private int maxDotCount = 6;

        private TMP_Text _loadingText;
        private Coroutine _animateCoroutine;
        private StringBuilder _stringBuilder;

        private void Awake()
        {
            _loadingText = GetComponent<TMP_Text>();

            int maxCapacity = baseText.Length + maxDotCount;
            _stringBuilder = new StringBuilder(maxCapacity);
        }

        private void OnEnable()
        {
            if (_animateCoroutine != null)
            {
                StopCoroutine(_animateCoroutine);
            }

            _animateCoroutine = StartCoroutine(LoadingText());
        }

        private void OnDisable()
        {
            if (_animateCoroutine != null)
            {
                StopCoroutine(_animateCoroutine);
                _animateCoroutine = null;
            }
        }

        private IEnumerator LoadingText()
        {
            int currentDotCount = 1;

            while (true)
            {
                _stringBuilder.Clear();

                _stringBuilder.Append(baseText);

                for (int i = 0; i < currentDotCount; i++)
                {
                    _stringBuilder.Append('.');
                }

                _loadingText.SetText(_stringBuilder);

                currentDotCount++;
                if (currentDotCount > maxDotCount)
                    currentDotCount = 1;

                yield return new WaitForSeconds(dotChangeInterval);
            }
        }
    }
}