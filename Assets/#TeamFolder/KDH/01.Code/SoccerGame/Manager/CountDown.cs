using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI text;
    
    [SerializeField] private float displayDuration = 1f; 

    [SerializeField] private GameObject[] stopObj;

    private void Start()
    {
        StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        foreach (var obj in stopObj)
            if (obj != null) obj.SetActive(false);

        string[] steps = { "3", "2", "1", "start"};

        foreach (string step in steps)
        {
            text.text = step;
            yield return new WaitForSeconds(displayDuration);
        }

        text.gameObject.SetActive(false);

        foreach (var obj in stopObj)
            if (obj != null) obj.SetActive(true);
    }
}

