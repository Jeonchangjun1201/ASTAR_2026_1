using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float displayDuration = 1f;
    [SerializeField] private float startDuration = 0.5f; // Start! 표시 시간
    [SerializeField] private GameObject[] stopObj;

    public static event Action OnCountdownFinished;

    private void Start()
    {
        StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        foreach (var obj in stopObj)
            if (obj != null) obj.SetActive(false);

        // 3, 2, 1은 displayDuration으로
        string[] countSteps = { "3", "2", "1" };
        foreach (string step in countSteps)
        {
            text.text = step;
            yield return new WaitForSeconds(displayDuration);
        }

        // Start!만 짧게
        text.text = "Start!";
        yield return new WaitForSeconds(startDuration);

        text.gameObject.SetActive(false);

        foreach (var obj in stopObj)
            if (obj != null) obj.SetActive(true);

        OnCountdownFinished?.Invoke();
    }
}