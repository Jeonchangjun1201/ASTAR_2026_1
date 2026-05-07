using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Battle
{
    public class BattleLeaderboardUI : MonoBehaviour
    {
        private Text _ruleText;
        private Text _teamOneScoreText;
        private Text _teamTwoScoreText;
        private Text _teamOneReasonText;
        private Text _teamTwoReasonText;
        private Text _teamOnePlayersText;
        private Text _teamTwoPlayersText;
        private Text _winnerText;
        private Image _winnerPanel;

        private void Awake()
        {
            Build();
            ShowWinner(string.Empty, Color.clear, false);
        }

        public void UpdateBoard(
            string teamOneName,
            Color teamOneColor,
            int teamOneScore,
            int teamOneTarget,
            string teamOneReason,
            string teamOnePlayers,
            string teamTwoName,
            Color teamTwoColor,
            int teamTwoScore,
            int teamTwoTarget,
            string teamTwoReason,
            string teamTwoPlayers)
        {
            Build();
            _ruleText.text = "GOAL SCORE = 1st(+5) / 2nd(+4) / 3rd(+3) / 4th(+2)";
            _teamOneScoreText.text = $"{teamOneName} {teamOneScore}/{teamOneTarget}";
            _teamOneScoreText.color = teamOneColor;
            _teamOneReasonText.text = teamOneReason;
            _teamOnePlayersText.text = teamOnePlayers;
            _teamTwoScoreText.text = $"{teamTwoName} {teamTwoScore}/{teamTwoTarget}";
            _teamTwoScoreText.color = teamTwoColor;
            _teamTwoReasonText.text = teamTwoReason;
            _teamTwoPlayersText.text = teamTwoPlayers;
        }

        public void ShowWinner(string message, Color color, bool visible)
        {
            if (_winnerPanel == null || _winnerText == null) return;
            _winnerPanel.gameObject.SetActive(visible);
            _winnerText.gameObject.SetActive(visible);
            if (!visible) return;
            _winnerPanel.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.92f);
            _winnerText.color = color;
            _winnerText.text = message;
        }

        private void Build()
        {
            if (_teamOneScoreText != null) return;

            var canvasObject = new GameObject("BattleLeaderboardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 650;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _ruleText = MakeText(canvasObject.transform, "RuleText", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            var ruleRt = _ruleText.GetComponent<RectTransform>();
            ruleRt.anchorMin = new Vector2(0.5f, 1f);
            ruleRt.anchorMax = new Vector2(0.5f, 1f);
            ruleRt.pivot = new Vector2(0.5f, 1f);
            ruleRt.anchoredPosition = new Vector2(0f, -8f);
            ruleRt.sizeDelta = new Vector2(760f, 28f);
            _ruleText.color = new Color(0.9f, 0.92f, 0.98f, 0.92f);

            var leftPanel = BuildCornerTeamPanel(
                canvasObject.transform,
                "LeftPanel",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(14f, -42f),
                out _teamOneScoreText,
                out _teamOneReasonText,
                out _teamOnePlayersText);
            var rightPanel = BuildCornerTeamPanel(
                canvasObject.transform,
                "RightPanel",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-14f, -42f),
                out _teamTwoScoreText,
                out _teamTwoReasonText,
                out _teamTwoPlayersText);
            leftPanel.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.12f, 0.78f);
            rightPanel.GetComponent<Image>().color = new Color(0.12f, 0.05f, 0.05f, 0.78f);

            var winnerPanelRect = MakeRect(canvasObject.transform, "WinnerPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            winnerPanelRect.sizeDelta = new Vector2(720f, 120f);
            _winnerPanel = winnerPanelRect.gameObject.AddComponent<Image>();
            _winnerPanel.raycastTarget = false;

            _winnerText = MakeText(winnerPanelRect, "WinnerText", 44, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(_winnerText.rectTransform, new Vector2(24f, 18f), new Vector2(-24f, -18f));
        }

        private static RectTransform BuildCornerTeamPanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            out Text scoreText,
            out Text reasonText,
            out Text playersText)
        {
            var panel = MakeRect(parent, name, anchorMin, anchorMax, pivot);
            panel.sizeDelta = new Vector2(380f, 230f);
            panel.anchoredPosition = anchoredPosition;
            var image = panel.gameObject.AddComponent<Image>();
            image.raycastTarget = false;

            scoreText = MakeText(panel, "ScoreText", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(scoreText.rectTransform, new Vector2(14f, -46f), new Vector2(-14f, -10f));

            reasonText = MakeText(panel, "ReasonText", 15, FontStyle.Normal, TextAnchor.MiddleCenter);
            reasonText.horizontalOverflow = HorizontalWrapMode.Wrap;
            reasonText.verticalOverflow = VerticalWrapMode.Overflow;
            reasonText.color = new Color(0.9f, 0.9f, 0.9f);
            Stretch(reasonText.rectTransform, new Vector2(14f, -84f), new Vector2(-14f, -46f));

            playersText = MakeText(panel, "PlayersText", 18, FontStyle.Normal, TextAnchor.UpperLeft);
            playersText.horizontalOverflow = HorizontalWrapMode.Wrap;
            playersText.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(playersText.rectTransform, new Vector2(16f, -212f), new Vector2(-16f, -94f));

            return panel;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(offsetMin.x, offsetMax.y);
            rect.offsetMax = new Vector2(offsetMax.x, offsetMin.y);
        }

        private static RectTransform MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            return rect;
        }

        private static Text MakeText(Transform parent, string name, int fontSize, FontStyle style, TextAnchor alignment)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
