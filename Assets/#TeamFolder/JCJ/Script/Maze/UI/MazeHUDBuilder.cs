using TMPro;
using UnityEngine;
using UnityEngine.UI;

//  미로 HUD를 코드로 생성하고 조립하는 빌더.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 런타임 전용 HUD 빌더. 표시 문자열은 TMP 기본 에셋 한글 폴백 이슈를 피하려고 영문 유지.
    /// 어두운 반투명 패널 + 차가운 단색(흑백 느낌) 액센트.
    /// </summary>
    public class MazeHUDBuilder : MonoBehaviour
    {
        public TextMeshProUGUI TimerText { get; private set; }
        public TextMeshProUGUI RankFeedText { get; private set; }
        public TextMeshProUGUI ScoreText { get; private set; }
        public TextMeshProUGUI CountdownText { get; private set; }
        public Slider StaminaSlider { get; private set; }
        public Image StaminaFill { get; private set; }
        public GameObject ResultPanel { get; private set; }
        public TextMeshProUGUI ResultText { get; private set; }
        public Button RestartButton { get; private set; }
        public Transform RestartButtonVisual { get; private set; }
        public CanvasGroup ResultCanvasGroup { get; private set; }
        public Canvas Canvas { get; private set; }

        public void Build()
        {
            Canvas = FindOrCreateCanvas();

            CountdownText ??= BuildCountdownText(Canvas.transform);
            TimerText     ??= BuildTimerText(Canvas.transform);
            ScoreText     ??= BuildScoreText(Canvas.transform);
            RankFeedText  ??= BuildRankFeedText(Canvas.transform);
            if (StaminaSlider == null) (StaminaSlider, StaminaFill) = BuildStaminaSlider(Canvas.transform);
            if (ResultPanel == null)    BuildResultPanel(Canvas.transform);
        }

        // ── UI 요소 ───────────────────────────────────
        private TextMeshProUGUI BuildCountdownText(Transform parent)
        {
            var go = CreateText("CountdownText", parent, TextAlignmentOptions.Center, 220, JCJUiColors.HudAccentBright, FontStyles.Bold);
            go.outlineColor = Color.black;
            go.outlineWidth = 0.25f;
            var rt = go.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900, 360);
            go.text = "";
            go.raycastTarget = false;
            go.transform.localScale = Vector3.zero;
            return go;
        }

        private TextMeshProUGUI BuildTimerText(Transform parent)
        {
            var panel = CreatePanel("TimerPanel", parent, new Vector2(280, 88));
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            panel.rectTransform.pivot = new Vector2(0.5f, 1f);
            panel.rectTransform.anchoredPosition = new Vector2(0, -28);

            var t = CreateText("TimerText", panel.transform, TextAlignmentOptions.Center, 56, JCJUiColors.HudPrimaryText, FontStyles.Bold);
            Stretch(t.rectTransform);
            t.text = "00:00";
            t.raycastTarget = false;
            ApplyTextOutline(t);
            t.characterSpacing = 2f;
            return t;
        }

        private TextMeshProUGUI BuildScoreText(Transform parent)
        {
            var panel = CreatePanel("ScorePanel", parent, new Vector2(280, 72));
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0f, 1f);
            panel.rectTransform.pivot = new Vector2(0f, 1f);
            panel.rectTransform.anchoredPosition = new Vector2(22, -26);

            var t = CreateText("ScoreText", panel.transform, TextAlignmentOptions.MidlineLeft, 32, JCJUiColors.HudAccentBright, FontStyles.Bold);
            Stretch(t.rectTransform, 18);
            t.text = "SCORE  0";
            t.raycastTarget = false;
            ApplyTextOutline(t);
            return t;
        }

        private TextMeshProUGUI BuildRankFeedText(Transform parent)
        {
            var panel = CreatePanel("RankFeedPanel", parent, new Vector2(340, 268));
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(1f, 1f);
            panel.rectTransform.pivot = new Vector2(1f, 1f);
            panel.rectTransform.anchoredPosition = new Vector2(-22, -26);

            var title = CreateText("Title", panel.transform, TextAlignmentOptions.TopLeft, 18, JCJUiColors.HudAccent, FontStyles.Bold | FontStyles.UpperCase);
            title.rectTransform.anchorMin = new Vector2(0, 1);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.offsetMin = new Vector2(14, -30);
            title.rectTransform.offsetMax = new Vector2(-14, -6);
            title.text = "LEADERBOARD";
            title.raycastTarget = false;
            title.characterSpacing = 1.2f;

            var t = CreateText("RankFeedText", panel.transform, TextAlignmentOptions.TopLeft, 25, JCJUiColors.HudPrimaryText, FontStyles.Normal);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(14, 14);
            rt.offsetMax = new Vector2(-14, -36);
            t.text = "";
            t.raycastTarget = false;
            ApplyTextOutline(t, 0.12f);
            return t;
        }

        private (Slider, Image) BuildStaminaSlider(Transform parent)
        {
            var panel = CreatePanel("StaminaPanel", parent, new Vector2(360, 52));
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0f, 0f);
            panel.rectTransform.pivot = new Vector2(0f, 0f);
            panel.rectTransform.anchoredPosition = new Vector2(22, 22);

            var label = CreateText("Label", panel.transform, TextAlignmentOptions.MidlineLeft, 15, JCJUiColors.HudMutedText, FontStyles.Bold | FontStyles.UpperCase);
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0, 1);
            lrt.anchorMax = new Vector2(1, 1);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.offsetMin = new Vector2(10, -20);
            lrt.offsetMax = new Vector2(-10, -2);
            label.text = "STAMINA";

            var sliderGo = new GameObject("StaminaSlider");
            sliderGo.transform.SetParent(panel.transform, false);
            var rt = sliderGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(-20, 14);
            rt.anchoredPosition = new Vector2(0, 6);

            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            var bg = new GameObject("Background");
            bg.transform.SetParent(sliderGo.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.72f);
            Stretch(bgImg.rectTransform);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRT = fillArea.AddComponent<RectTransform>();
            Stretch(fillAreaRT, 2);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.75f, 0.88f, 1.00f, 1f);
            Stretch(fillImg.rectTransform);

            slider.fillRect = fillImg.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
            return (slider, fillImg);
        }

        private void BuildResultPanel(Transform parent)
        {
            var go = new GameObject("ResultPanel");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Stretch(rt);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.06f, 0.82f);
            bg.raycastTarget = true;

            var group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = true;
            ResultCanvasGroup = group;

            var card = CreatePanel("Card", go.transform, new Vector2(800, 540));
            card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.rectTransform.anchoredPosition = Vector2.zero;

            var title = CreateText("Title", card.transform, TextAlignmentOptions.Top, 52, JCJUiColors.HudPrimaryText, FontStyles.Bold | FontStyles.UpperCase);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.offsetMin = new Vector2(24, -118);
            trt.offsetMax = new Vector2(-24, -36);
            title.text = "Finished!";
            ApplyTextOutline(title, 0.14f);
            title.characterSpacing = 2f;

            var result = CreateText("ResultText", card.transform, TextAlignmentOptions.Center, 28, JCJUiColors.HudMutedText, FontStyles.Normal);
            var resRT = result.rectTransform;
            resRT.anchorMin = new Vector2(0, 0); resRT.anchorMax = new Vector2(1, 1);
            resRT.offsetMin = new Vector2(44, 128);
            resRT.offsetMax = new Vector2(-44, -158);
            result.raycastTarget = false;
            ResultText = result;

            var btnGo = new GameObject("RestartButton");
            btnGo.transform.SetParent(card.transform, false);
            var brt = btnGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0f); brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0, 44);
            brt.sizeDelta = new Vector2(300, 76);

            var bImg = btnGo.AddComponent<Image>();
            bImg.color = new Color(0.88f, 0.92f, 0.99f, 1f);
            var button = btnGo.AddComponent<Button>();
            button.targetGraphic = bImg;
            RestartButton = button;
            RestartButtonVisual = btnGo.transform;

            var colors = button.colors;
            colors.normalColor      = bImg.color;
            colors.highlightedColor = JCJUiColors.HudAccentBright;
            colors.pressedColor     = new Color(0.72f, 0.78f, 0.90f);
            colors.selectedColor    = JCJUiColors.HudAccentBright;
            button.colors = colors;

            var label = CreateText("Label", btnGo.transform, TextAlignmentOptions.Center, 26, new Color(0.06f, 0.08f, 0.12f), FontStyles.Bold | FontStyles.UpperCase);
            Stretch(label.rectTransform);
            label.text = "Play Again";
            label.raycastTarget = false;

            HudTweenHelpers.ButtonHover(button, btnGo.transform);

            go.SetActive(false);
            ResultPanel = go;
        }

        // ── 헬퍼 ─────────────────────────────────────
        private static TextMeshProUGUI CreateText(string name, Transform parent, TextAlignmentOptions align, float size, Color color, FontStyles style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.alignment = align;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            return t;
        }

        private static Image CreatePanel(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = JCJUiColors.HudPanel;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = size;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = JCJUiColors.HudShadow;
            shadow.effectDistance = new Vector2(6f, -6f);

            // 상단 얇은 액센트 막대(카드 상단 테두리 느낌).
            var accent = new GameObject("AccentLine");
            accent.transform.SetParent(go.transform, false);
            var accentRT = accent.AddComponent<RectTransform>();
            accentRT.anchorMin = new Vector2(0f, 1f);
            accentRT.anchorMax = new Vector2(1f, 1f);
            accentRT.pivot = new Vector2(0.5f, 1f);
            accentRT.offsetMin = new Vector2(10f, -3f);
            accentRT.offsetMax = new Vector2(-10f, 0f);
            var accentImg = accent.AddComponent<Image>();
            accentImg.color = JCJUiColors.HudAccentLine;
            accentImg.raycastTarget = false;

            return img;
        }

        private static void ApplyTextOutline(TextMeshProUGUI t, float width = 0.18f)
        {
            if (t == null) return;
            t.outlineWidth = width;
            t.outlineColor = JCJUiColors.HudTextOutline;
        }

        private static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private Canvas FindOrCreateCanvas()
        {
            var c = Object.FindFirstObjectByType<Canvas>();
            if (c != null) return c;
            var go = new GameObject("Canvas (auto)");
            c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return c;
        }
    }
}
