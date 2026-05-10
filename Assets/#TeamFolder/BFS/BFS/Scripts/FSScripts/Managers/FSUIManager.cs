using System;
using System.Collections;
using TMPro;
using UnityEngine;
namespace BFS
{
    public class FSUIManager : MonoBehaviour
    {
        [field: SerializeField] public TextMeshProUGUI CountDownText { get; private set; }
        [field: SerializeField] public TextMeshProUGUI ColorText { get; private set; }
        [field: SerializeField] public TextMeshProUGUI GameOverText { get; private set; }

        public void ChangeText(TextMeshProUGUI text, string content, float duration)
        {
            text.text = content;
            StartCoroutine(TextDurationCoroutine(text, duration));
        }

        private IEnumerator TextDurationCoroutine(TextMeshProUGUI text, float duration)
        {
            if (duration == 0)
                duration = int.MaxValue;
            yield return new WaitForSeconds(duration);
            text.text = null;
        }
    }
}

