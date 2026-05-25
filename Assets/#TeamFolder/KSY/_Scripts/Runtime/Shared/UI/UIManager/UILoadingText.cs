using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace KSY.Shared.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class UILoadingText : MonoBehaviour
    {
        [SerializeField] private string baseText;
        [SerializeField] private float dotChangeInterval = 0.5f;
        [SerializeField] private int maxDotCount = 6;

        private TMP_Text _loadingText;
        private Coroutine _animateCoroutine;
        private StringBuilder _stringBuilder;

        public bool Initialized {get; private set;}

        public void Initialize()
        {
            if (Initialized) return;

            _loadingText = GetComponent<TMP_Text>();
            int maxCapacity = baseText.Length + maxDotCount;
            _stringBuilder = new StringBuilder(maxCapacity);

            Initialized = true;
        }

        public void SetText(string baseText) => this.baseText = baseText;

        public void Load()
        {
            if (_animateCoroutine != null)
            {
                StopCoroutine(_animateCoroutine);
            }

            _animateCoroutine = StartCoroutine(LoadingText());
        }


        public void Load(string info)
        {
            baseText = info;

            if (_animateCoroutine != null)
            {
                StopCoroutine(_animateCoroutine);
            }

            _animateCoroutine = StartCoroutine(LoadingText());
        }

        public void Unload()
        {
            if (_animateCoroutine != null)
            {
                StopCoroutine(_animateCoroutine);
                _animateCoroutine = null;
            }
        }

        private IEnumerator LoadingText()
        {
            if (_animateCoroutine == null) yield break;

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