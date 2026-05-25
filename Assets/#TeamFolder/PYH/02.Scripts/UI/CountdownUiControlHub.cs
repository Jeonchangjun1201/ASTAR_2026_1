using System.Collections;
using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class CountdownUi : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform labelTrans;

        [Header("Text")]
        [SerializeField] private float maxFontSize = 400f;

        [Header("Time")]
        [SerializeField] private float oneCountDuration = 1f;
        [SerializeField] private float appearDuration = 0.25f;
        [SerializeField] private float hideDuration = 0.25f;

        [Header("Colors")]
        [SerializeField] private Color count3Color;
        [SerializeField] private Color count2Color;
        [SerializeField] private Color count1Color;
        [SerializeField] private Color goColor;

        private Sequence _seq;

        private void Awake()
        {
            label.text = "";
            label.fontSize = 0f;
            labelTrans.localRotation = Quaternion.identity;
            
            AStarEventBus.Subscribe<CountdownUiEvent>(PlayCountdown);
        }
        private void OnDestroy()
        {
            AStarEventBus.Unsubscribe<CountdownUiEvent>(PlayCountdown);
        }
        
        private void PlayCountdown(CountdownUiEvent @event)
        {
            StopAllCoroutines();
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            yield return PlayOne("3", count3Color, oneCountDuration);
            yield return PlayOne("2", count2Color, oneCountDuration);
            yield return PlayOne("1", count1Color, oneCountDuration);

            yield return PlayOne("GO!", goColor, oneCountDuration);
        }
        private IEnumerator PlayOne(string text, Color color, float totalDuration)
        {
            _seq?.Kill();

            label.text = text;
            label.color = color;
            label.fontSize = 0f;
            labelTrans.localRotation = Quaternion.identity;

            float stayDuration = totalDuration - appearDuration - hideDuration;
            if (stayDuration < 0f) stayDuration = 0f;

            _seq = DOTween.Sequence().SetUpdate(true);

            _seq.Append(DOTween.To(
                () => label.fontSize,
                x => label.fontSize = x,
                maxFontSize,
                appearDuration
            ).SetEase(Ease.OutBack));

            _seq.AppendInterval(stayDuration);

            _seq.Append(
                labelTrans
                    .DOLocalRotate(new Vector3(0f, 0f, -90f), hideDuration)
                    .SetEase(Ease.InBack)
            );

            _seq.Join(DOTween.To(
                () => label.fontSize,
                x => label.fontSize = x,
                0f,
                hideDuration
            ).SetEase(Ease.InBack));

            yield return _seq.WaitForCompletion();
        }
    }
}