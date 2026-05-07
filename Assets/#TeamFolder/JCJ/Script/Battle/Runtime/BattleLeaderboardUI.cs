using UnityEngine;
using UnityEngine.UI;

// 팀 점수와 플레이어 기록을 보여주는 리더보드 UI.

namespace _TeamFolder.JCJ.Battle
{
    public class BattleLeaderboardUI : MonoBehaviour
    {
        private RectTransform _leftPanel;
        private RectTransform _rightPanel;
        private Text _teamOneScoreText;
        private Text _teamTwoScoreText;
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
            string teamOnePlayers,
            string teamTwoName,
            Color teamTwoColor,
            int teamTwoScore,
            int teamTwoTarget,
            string teamTwoPlayers)
        {
            Build();
            _leftPanel.gameObject.SetActive(true);
            _rightPanel.gameObject.SetActive(true);
            _teamOneScoreText.text = $"{teamOneName} {teamOneScore}/{teamOneTarget}";
            _teamOneScoreText.color = teamOneColor;
            _teamOnePlayersText.text = teamOnePlayers;
            _teamTwoScoreText.text = $"{teamTwoName} {teamTwoScore}/{teamTwoTarget}";
            _teamTwoScoreText.color = teamTwoColor;
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
            canvas.sortingOrder = 2000;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _leftPanel = BuildCornerTeamPanel(
                canvasObject.transform,
                "LeftPanel",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(14f, -10f),
                out _teamOneScoreText,
                out _teamOnePlayersText);
            _rightPanel = BuildCornerTeamPanel(
                canvasObject.transform,
                "RightPanel",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-14f, -10f),
                out _teamTwoScoreText,
                out _teamTwoPlayersText);
            _leftPanel.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.12f, 0.88f);
            _rightPanel.GetComponent<Image>().color = new Color(0.12f, 0.05f, 0.05f, 0.88f);

            var winnerPanelRect = MakeRect(canvasObject.transform, "WinnerPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            winnerPanelRect.sizeDelta = new Vector2(720f, 120f);
            _winnerPanel = winnerPanelRect.gameObject.AddComponent<Image>();
            _winnerPanel.raycastTarget = false;

            _winnerText = MakeText(winnerPanelRect, "WinnerText", 44, FontStyle.Bold, TextAnchor.MiddleCenter);
            var winnerTextRect = _winnerText.rectTransform;
            winnerTextRect.anchorMin = Vector2.zero;
            winnerTextRect.anchorMax = Vector2.one;
            winnerTextRect.offsetMin = new Vector2(24f, 18f);
            winnerTextRect.offsetMax = new Vector2(-24f, -18f);
        }

        private static RectTransform BuildCornerTeamPanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            out Text scoreText,
            out Text playersText)
        {
            var panel = MakeRect(parent, name, anchorMin, anchorMax, pivot);
            panel.sizeDelta = new Vector2(320f, 150f);
            panel.anchoredPosition = anchoredPosition;
            var image = panel.gameObject.AddComponent<Image>();
            image.raycastTarget = false;

            scoreText = MakeText(panel, "ScoreText", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            var scoreRect = scoreText.rectTransform;
            scoreRect.anchorMin = new Vector2(0f, 1f);
            scoreRect.anchorMax = new Vector2(1f, 1f);
            scoreRect.offsetMin = new Vector2(12f, -44f);
            scoreRect.offsetMax = new Vector2(-12f, -8f);

            playersText = MakeText(panel, "PlayersText", 18, FontStyle.Normal, TextAnchor.UpperLeft);
            playersText.horizontalOverflow = HorizontalWrapMode.Wrap;
            playersText.verticalOverflow = VerticalWrapMode.Overflow;
            var playersRect = playersText.rectTransform;
            playersRect.anchorMin = new Vector2(0f, 0f);
            playersRect.anchorMax = new Vector2(1f, 1f);
            playersRect.offsetMin = new Vector2(14f, 12f);
            playersRect.offsetMax = new Vector2(-14f, -48f);

            return panel;
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
