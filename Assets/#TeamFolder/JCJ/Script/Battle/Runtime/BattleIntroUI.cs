using UnityEngine;
using UnityEngine.UI;

// BattlePrototypeScene 인트로. ShowWeaponRoll·ShowCountdown은 BattlePrototypeManager.BeginMatchRoutine에서만 호출된다.
// 서버 연동 시에는 네트워크 타임라인(예: MatchStartTick)에 맞춰 동일 API를 호출하거나, 서버 메시지를 받는 래퍼에서 이 컴포넌트를 호출하면 UI와 게임플레이 잠금 시점을 맞추기 쉽다.

namespace _TeamFolder.JCJ.Battle
{
    public class BattleIntroUI : MonoBehaviour
    {
        private CanvasGroup _group;
        private RectTransform _panel;
        private Image _teamStripe;
        private Text _phaseText;
        private Text _teamText;
        private Text _rankText;
        private Text _weaponText;
        private Text _gradeText;
        private Text _damageText;
        private Text _rpmText;
        private Text _ammoText;
        private Text _countdownText;

        private void Awake()
        {
            Build();
            Hide();
        }

        public void ShowWeaponRoll(string teamName, Color teamColor, int rank, BattleWeaponDefinition weapon, bool reveal)
        {
            if (weapon == null) return;
            Build();
            SetWeaponInfoVisible(true);
            _group.alpha = 1f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _panel.gameObject.SetActive(true);
            _teamStripe.color = teamColor;
            _phaseText.text = reveal ? "WEAPON READY" : "WEAPON DRAW";
            _teamText.text = teamName;
            _teamText.color = teamColor;
            _rankText.text = $"RANK {rank}";
            _weaponText.text = weapon.DisplayName;
            _gradeText.text = weapon.Grade.ToString().ToUpperInvariant();
            _damageText.text = $"DMG  {Mathf.RoundToInt(weapon.Damage)}";
            _rpmText.text = $"RPM  {Mathf.RoundToInt(60f / weapon.FireInterval)}";
            _ammoText.text = $"AMMO {weapon.MagazineSize}/{weapon.TotalAmmo}";
            _countdownText.text = string.Empty;
        }

        public void ShowCountdown(string teamName, Color teamColor, int seconds)
        {
            Build();
            SetWeaponInfoVisible(false);
            _group.alpha = 1f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _panel.gameObject.SetActive(true);
            _teamStripe.color = teamColor;
            _phaseText.text = "MATCH START";
            _teamText.text = teamName;
            _teamText.color = teamColor;
            _rankText.text = string.Empty;
            _weaponText.text = string.Empty;
            _gradeText.text = string.Empty;
            _countdownText.text = seconds > 0 ? seconds.ToString() : "GO";
        }

        public void Hide()
        {
            if (_group == null) return;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void Build()
        {
            if (_group != null) return;

            var canvasObject = new GameObject("BattleIntroCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _group = canvasObject.GetComponent<CanvasGroup>();

            _panel = MakeRect(canvasObject.transform, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _panel.sizeDelta = new Vector2(760f, 420f);
            var panelImage = _panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.04f, 0.06f, 0.88f);
            panelImage.raycastTarget = false;

            var stripe = MakeRect(_panel, "TeamStripe", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            stripe.sizeDelta = new Vector2(0f, 10f);
            stripe.anchoredPosition = Vector2.zero;
            _teamStripe = stripe.gameObject.AddComponent<Image>();
            _teamStripe.raycastTarget = false;

            _phaseText = MakeText(_panel, "PhaseText", 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(_phaseText.rectTransform, new Vector2(28f, -64f), new Vector2(-28f, -20f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            _phaseText.color = new Color(0.95f, 0.95f, 0.95f);

            _weaponText = MakeText(_panel, "WeaponText", 38, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(_weaponText.rectTransform, new Vector2(40f, -162f), new Vector2(-40f, -88f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            _weaponText.color = Color.white;

            _teamText = MakeText(_panel, "TeamText", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            Stretch(_teamText.rectTransform, new Vector2(42f, -214f), new Vector2(260f, -172f), new Vector2(0f, 1f), new Vector2(0f, 1f));

            _rankText = MakeText(_panel, "RankText", 18, FontStyle.Bold, TextAnchor.MiddleRight);
            Stretch(_rankText.rectTransform, new Vector2(-260f, -214f), new Vector2(-42f, -172f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            _rankText.color = new Color(0.95f, 0.88f, 0.42f);

            _gradeText = MakeText(_panel, "GradeText", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(_gradeText.rectTransform, new Vector2(36f, -258f), new Vector2(-36f, -220f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            _gradeText.color = new Color(0.78f, 0.84f, 0.96f);

            _damageText = MakeText(_panel, "DamageText", 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(_damageText.rectTransform, new Vector2(52f, 112f), new Vector2(-52f, 152f), new Vector2(0f, 0f), new Vector2(1f, 0f));

            _rpmText = MakeText(_panel, "RpmText", 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(_rpmText.rectTransform, new Vector2(52f, 68f), new Vector2(-52f, 108f), new Vector2(0f, 0f), new Vector2(1f, 0f));

            _ammoText = MakeText(_panel, "AmmoText", 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(_ammoText.rectTransform, new Vector2(52f, 24f), new Vector2(-52f, 64f), new Vector2(0f, 0f), new Vector2(1f, 0f));

            _countdownText = MakeText(_panel, "CountdownText", 92, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(_countdownText.rectTransform, new Vector2(40f, 160f), new Vector2(-40f, 300f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            _countdownText.color = new Color(1f, 0.95f, 0.95f);
        }

        private void SetWeaponInfoVisible(bool visible)
        {
            if (_weaponText != null) _weaponText.gameObject.SetActive(visible);
            if (_rankText != null) _rankText.gameObject.SetActive(visible);
            if (_gradeText != null) _gradeText.gameObject.SetActive(visible);
            if (_damageText != null) _damageText.gameObject.SetActive(visible);
            if (_rpmText != null) _rpmText.gameObject.SetActive(visible);
            if (_ammoText != null) _ammoText.gameObject.SetActive(visible);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
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
