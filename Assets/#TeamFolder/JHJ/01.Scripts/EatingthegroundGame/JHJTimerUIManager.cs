using UnityEngine;
using TMPro;
using JHJ.Scripts.EatingthegroundGame;
using PYH.Util;

public class JHJTimerUIManager : MonoSingleton<JHJTimerUIManager>
{
    [SerializeField] private TextMeshProUGUI _timerText;

    private void OnEnable()
    {
        if (JHJPaintingGameTimerManager.Instance != null)
        {
            JHJPaintingGameTimerManager.Instance.OnReadyTimeUpdated += UpdateTimerText;
            JHJPaintingGameTimerManager.Instance.OnTimeUpdated += UpdateTimerText;
        }
    }

    private void OnDisable()
    {
        if (JHJPaintingGameTimerManager.Instance != null)
        {
            JHJPaintingGameTimerManager.Instance.OnReadyTimeUpdated -= UpdateTimerText;
            JHJPaintingGameTimerManager.Instance.OnTimeUpdated -= UpdateTimerText;
        }
    }


    //Ui 메서드
    private void UpdateTimerText(float time)
    {
        if (_timerText != null)
        {
            _timerText.text = Mathf.CeilToInt(time).ToString();
        }
    }
}