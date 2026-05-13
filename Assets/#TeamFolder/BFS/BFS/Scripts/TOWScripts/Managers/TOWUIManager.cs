using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace BFS
{
    public class TOWUIManager : MonoBehaviour
    {
        [field: SerializeField] public TextMeshProUGUI TeamOneText;
        [field: SerializeField] public TextMeshProUGUI TeamTwoText;
        [field: SerializeField] public TextMeshProUGUI TimerText;
        [field: SerializeField] public TextMeshProUGUI GameOverText;
        [field: SerializeField] public TextMeshProUGUI GoalText;
        [field: SerializeField] public Slider TeamSlider;
        public void ChangeText(TextMeshProUGUI text, string content)
        {
            text.text = content;
        }
        public void ChangeText(TextMeshProUGUI text, string content, float duration)
        {
            text.text = content;
            StartCoroutine(TextDurationCoroutine(text, duration));
        }
        public void AddValue(ITeamTOW team, bool isCorrect)
        {
            float val = isCorrect ? 0.02f : -0.02f;
            if (team.Team == PlayerTeamTOW.TEAMONE)
                TeamSlider.value += val;
            else
                TeamSlider.value -= val;
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
