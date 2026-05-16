using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD 애니메이션과 트윈 동작을 돕는 유틸.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// HUD 요소마다 같은 느낌의 DOTween 효과를 쓰기 위한 공통 헬퍼.
    /// </summary>
    public static class HudTweenHelpers
    {
        public static void PunchScale(Transform t, float strength = 0.25f, float duration = 0.3f)
        {
            if (t == null) return;
            t.DOKill(true);
            t.localScale = Vector3.one;
            t.DOPunchScale(Vector3.one * strength, duration, vibrato: 6, elasticity: 0.8f);
        }

        public static void BounceCountdown(Transform t)
        {
            if (t == null) return;
            t.DOKill(true);
            t.localScale = Vector3.zero;
            var seq = DOTween.Sequence();
            seq.Append(t.DOScale(1.4f, 0.2f).SetEase(Ease.OutBack));
            seq.Append(t.DOScale(1.0f, 0.2f).SetEase(Ease.InOutSine));
            seq.AppendInterval(0.4f);
            seq.Append(t.DOScale(0.2f, 0.2f).SetEase(Ease.InBack));
        }

        public static void GoBurst(Transform t, TextMeshProUGUI text)
        {
            if (t == null) return;
            t.DOKill(true);
            t.localScale = Vector3.zero;
            if (text != null) text.color = new Color(1f, 0.95f, 0.4f, 1f);

            var seq = DOTween.Sequence();
            seq.Append(t.DOScale(2.0f, 0.25f).SetEase(Ease.OutBack));
            seq.Append(t.DOScale(1.0f, 0.2f).SetEase(Ease.InOutSine));
            seq.AppendInterval(0.3f);
            seq.Append(t.DOScale(0f, 0.25f).SetEase(Ease.InBack));
        }

        public static void PulseRed(TextMeshProUGUI text)
        {
            if (text == null) return;
            text.DOKill(true);
            text.DOColor(new Color(1f, 0.25f, 0.25f), 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        public static void StopColorLoop(TextMeshProUGUI text, Color baseline)
        {
            if (text == null) return;
            text.DOKill(true);
            text.color = baseline;
        }

        public static void ShowPanel(CanvasGroup group, float duration = 0.35f)
        {
            if (group == null) return;
            group.gameObject.SetActive(true);
            group.DOKill(true);
            group.alpha = 0f;
            group.DOFade(1f, duration).SetEase(Ease.OutQuad);
        }

        public static void HidePanel(CanvasGroup group, float duration = 0.25f)
        {
            if (group == null) return;
            group.DOKill(true);
            group.DOFade(0f, duration).SetEase(Ease.InQuad)
                .OnComplete(() => group.gameObject.SetActive(false));
        }

        public static void FillTween(Slider slider, float target, float duration = 0.15f)
        {
            if (slider == null) return;
            slider.DOKill(true);
            DOTween.To(() => slider.value, v => slider.value = v, target, duration)
                   .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// 마지막으로 보인 정수(점수 등)에서 <paramref name="targetValue"/>까지 부드럽게 보간.
        /// 전달한 TMP에 DOTween 키를 써서 연속 호출 시 이전 트윈을 깔끔히 덮어쓴다.
        /// </summary>
        public static void TickUpNumber(TextMeshProUGUI text, int targetValue, string prefix = "",
                                        float duration = 0.35f)
        {
            if (text == null) return;
            DOTween.Kill(text, true);

            int startValue = 0;
            if (!string.IsNullOrEmpty(text.text))
            {
                // 현재 표시 문자열에서 숫자만 뽑아 시작값을 안정적으로 읽는다.
                var digits = new System.Text.StringBuilder();
                foreach (var ch in text.text) if (ch >= '0' && ch <= '9') digits.Append(ch);
                if (digits.Length > 0) int.TryParse(digits.ToString(), out startValue);
            }

            int value = startValue;
            DOTween.To(() => value, v =>
                      {
                          value = v;
                          text.text = string.IsNullOrEmpty(prefix) ? v.ToString() : $"{prefix}{v}";
                      }, targetValue, duration)
                   .SetEase(Ease.OutCubic)
                   .SetId(text)
                   .SetUpdate(true);
        }

        /// <summary>
        /// 짧은 전체 화면 틴트 플래시. 최상위 캔버스에 오버레이 Image가 없으면 만든다.
        /// 작은 획득 피드백용.
        /// </summary>
        public static void FlashFullscreen(Color tint, float duration = 0.2f, float maxAlpha = 0.35f)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("__HudFlash") as RectTransform;
            Image img;
            if (existing == null)
            {
                var go = new GameObject("__HudFlash");
                go.transform.SetParent(canvas.transform, false);
                img = go.AddComponent<Image>();
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                img = existing.GetComponent<Image>();
            }

            if (img == null) return;
            var col = tint; col.a = maxAlpha;
            img.DOKill(true);
            img.color = col;
            img.DOFade(0f, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        public static void ButtonHover(Button btn, Transform target)
        {
            if (btn == null || target == null) return;
            var trigger = btn.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>()
                          ?? btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => target.DOScale(1.08f, 0.15f).SetEase(Ease.OutBack));
            trigger.triggers.Add(enter);

            var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => target.DOScale(1.0f, 0.15f).SetEase(Ease.OutQuad));
            trigger.triggers.Add(exit);
        }
    }
}
