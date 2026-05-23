using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 완주 결과를 포디움 형태로 보여주는 프리젠터.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 게임 종료 시 뜨는 포디움/시상 화면.
    /// 랭크 서비스가 OnAllFinished를 쏠 때 표시(상위 3명 확정 또는 타이머 만료).
    /// 상위 3명 점수 표시, PlayerPrefs 누적 총점을 DOTween으로 카운트업, 1위 비주얼에 승리 연출.
    /// </summary>
    public class PodiumPresenter : MonoBehaviour
    {
        private const string TotalScorePrefKey = "MazeTotalScore";

        [Header("런타임 생성(전부 자동)")]
        [SerializeField] private Canvas _canvas;

        [Header("총점 카운트업")]
        [Tooltip("이전 총점 → 새 총점까지 숫자가 올라가는 데 걸리는 시간(초).")]
        [SerializeField] private float _countDuration = 1.4f;
        [Tooltip("카운트업 애니메이션 이징.")]
        [SerializeField] private Ease _countEase = Ease.OutCubic;

        // 런타임에 만든 UI
        private CanvasGroup _group;
        private TextMeshProUGUI _titleText;
        private readonly TextMeshProUGUI[] _slotName  = new TextMeshProUGUI[3];
        private readonly TextMeshProUGUI[] _slotScore = new TextMeshProUGUI[3];
        private readonly RectTransform[]   _slotRoot  = new RectTransform[3];
        private TextMeshProUGUI _earnedText;
        private TextMeshProUGUI _totalText;
        private Tween _counterTween;

        private void Awake()
        {
            _canvas ??= GetComponent<Canvas>();
            if (_canvas == null)
            {
                var hudRoot = GameObject.Find("HUD (auto)");
                if (hudRoot != null) _canvas = hudRoot.GetComponent<Canvas>();
            }
            if (_canvas == null) _canvas = CreateCanvas();
            BuildUI();
            HideInstant();
        }

        private void Start()
        {
            if (GameStateManager.Instance?.Rank != null)
                GameStateManager.Instance.Rank.OnAllFinished += HandleFinished;

            var gsm = GameStateManager.Instance;
            if (gsm != null) gsm.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance?.Rank != null)
                GameStateManager.Instance.Rank.OnAllFinished -= HandleFinished;

            var gsm = GameStateManager.Instance;
            if (gsm != null) gsm.OnStateChanged -= OnStateChanged;
            _counterTween?.Kill();
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Waiting) HideInstant();
        }

        // ── 표시·채우기 ───────────────────────────────
        // 최종 랭킹을 받아 포디움 UI를 채우고 연출을 시작하는 진입점이다.
        // 서버에서 결과 패킷을 받는 구조가 되면 이 메서드가 그대로 최종 UI 반영 창구가 된다.
        private void HandleFinished(List<PlayerRankData> rankings)
        {
            PopulatePodium(rankings);
            AnimateScoreCountUp(rankings);
            PlayWinnerAnimation(rankings);
            ShowPanel();
        }

        private void PopulatePodium(List<PlayerRankData> rankings)
        {
            for (int i = 0; i < 3; i++)
            {
                if (_slotName[i] == null) continue;
                if (i < rankings.Count)
                {
                    _slotRoot[i].gameObject.SetActive(true);
                    _slotName[i].text  = rankings[i].PlayerName;
                    _slotScore[i].text = $"+{rankings[i].Score}";
                }
                else
                {
                    _slotRoot[i].gameObject.SetActive(false);
                }
            }
        }

        // 이번 판 점수를 누적 총점 UI로 반영하는 로컬 연출 단계다.
        // 서버 저장 총점과 연결하면 PlayerPrefs 대신 서버 응답값을 여기로 넣는 식으로 바꾸기 쉽다.
        private void AnimateScoreCountUp(List<PlayerRankData> rankings)
        {
            int earned = 0;
            foreach (var r in rankings) earned += r.Score;

            int oldTotal = PlayerPrefs.GetInt(TotalScorePrefKey, 0);
            int newTotal = oldTotal + earned;
            PlayerPrefs.SetInt(TotalScorePrefKey, newTotal);
            PlayerPrefs.Save();

            if (_earnedText != null) _earnedText.text = $"THIS RUN  +{earned}";
            if (_totalText != null)
            {
                _totalText.text = oldTotal.ToString("N0");
                _counterTween?.Kill();
                float current = oldTotal;
                _counterTween = DOTween.To(
                    () => current,
                    v =>
                    {
                        current = v;
                        _totalText.text = Mathf.RoundToInt(v).ToString("N0");
                    },
                    newTotal,
                    _countDuration)
                    .SetEase(_countEase)
                    .OnComplete(() => _totalText.text = newTotal.ToString("N0"));
                HudTweenHelpers.PunchScale(_totalText.transform, 0.18f, 0.4f);
            }
        }

        private void PlayWinnerAnimation(List<PlayerRankData> rankings)
        {
            if (rankings.Count == 0) return;
            string winnerId = rankings[0].PlayerId;
            string winnerName = rankings[0].PlayerName;
            var mm = MazeManager.Instance;
            if (mm == null) return;
            foreach (var go in mm.Players)
            {
                if (go == null) continue;
                var identity = RuntimePlayerIdentity.Find(go.transform);
                bool isWinner = identity != null
                    ? string.Equals(identity.PlayerId, winnerId, System.StringComparison.OrdinalIgnoreCase)
                    : go.name == winnerName;
                if (!isWinner) continue;
                var pc = go.GetComponent<PlayerController>();
                pc?.SetMovementEnabled(false);
                pc?.NotifyCollected();
                // 포디움 포즈로 카메라 쪽을 본다.
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 dir = cam.transform.position - go.transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(dir);
                }
                break;
            }
        }

        // ── 패널 페이드 ───────────────────────────────
        private void ShowPanel()
        {
            gameObject.SetActive(true);
            if (_group == null) return;
            _group.gameObject.SetActive(true);
            _group.DOKill(true);
            _group.alpha = 0f;
            _group.DOFade(1f, 0.4f).SetEase(Ease.OutQuad);
        }

        private void HideInstant()
        {
            if (_group == null) return;
            _group.DOKill(true);
            _group.alpha = 0f;
            _group.gameObject.SetActive(false);
            _counterTween?.Kill();
        }

        // ── UI 생성 ───────────────────────────────────
        private void BuildUI()
        {
            var root = new GameObject("PodiumRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(_canvas.transform, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var bg = root.GetComponent<Image>();
            bg.color = JCJUiColors.PodiumPanel;
            _group = root.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = true;

            _titleText = CreateText("Title", root.transform, TextAlignmentOptions.Top, 90, JCJUiColors.PodiumFirst,
                                    FontStyles.Bold | FontStyles.UpperCase);
            var ttr = _titleText.rectTransform;
            ttr.anchorMin = new Vector2(0, 1); ttr.anchorMax = new Vector2(1, 1);
            ttr.pivot = new Vector2(0.5f, 1f);
            ttr.offsetMin = new Vector2(0, -180); ttr.offsetMax = new Vector2(0, -40);
            _titleText.text = "PODIUM";

            // 3열: 왼쪽 2등, 가운데 1등(가장 높음), 오른쪽 3등.
            BuildSlot(0, root.transform, new Vector2(   0f, -100f), 1.00f, JCJUiColors.PodiumFirst, "1st");
            BuildSlot(1, root.transform, new Vector2(-340f, -160f), 0.85f, JCJUiColors.PodiumSecond, "2nd");
            BuildSlot(2, root.transform, new Vector2( 340f, -160f), 0.75f, JCJUiColors.PodiumThird, "3rd");

            _earnedText = CreateText("EarnedText", root.transform, TextAlignmentOptions.Center, 42, JCJUiColors.PodiumBody,
                                     FontStyles.Bold);
            var er = _earnedText.rectTransform;
            er.anchorMin = er.anchorMax = new Vector2(0.5f, 0f);
            er.pivot = new Vector2(0.5f, 0f);
            er.anchoredPosition = new Vector2(0, 260);
            er.sizeDelta = new Vector2(900, 60);
            _earnedText.text = "THIS RUN  +0";

            var totalLabel = CreateText("TotalLabel", root.transform, TextAlignmentOptions.Center, 28, JCJUiColors.PodiumMuted,
                                        FontStyles.Bold | FontStyles.UpperCase);
            var tlr = totalLabel.rectTransform;
            tlr.anchorMin = tlr.anchorMax = new Vector2(0.5f, 0f);
            tlr.pivot = new Vector2(0.5f, 0f);
            tlr.anchoredPosition = new Vector2(0, 200);
            tlr.sizeDelta = new Vector2(900, 40);
            totalLabel.text = "TOTAL SCORE";

            _totalText = CreateText("TotalText", root.transform, TextAlignmentOptions.Center, 96, JCJUiColors.PodiumBody,
                                    FontStyles.Bold);
            _totalText.outlineColor = Color.black;
            _totalText.outlineWidth = 0.18f;
            var ttlr = _totalText.rectTransform;
            ttlr.anchorMin = ttlr.anchorMax = new Vector2(0.5f, 0f);
            ttlr.pivot = new Vector2(0.5f, 0f);
            ttlr.anchoredPosition = new Vector2(0, 100);
            ttlr.sizeDelta = new Vector2(900, 100);
            _totalText.text = "0";
        }

        private void BuildSlot(int idx, Transform parent, Vector2 anchoredPos, float heightScale, Color badgeColour, string medal)
        {
            var go = new GameObject($"Slot_{medal}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = anchoredPos;
            float baseH = 260f;
            rt.sizeDelta = new Vector2(300, baseH * heightScale);
            _slotRoot[idx] = rt;

            var body = new GameObject("Body", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            body.transform.SetParent(go.transform, false);
            var brt = (RectTransform)body.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            var bimg = body.GetComponent<Image>();
            bimg.color = new Color(0.13f, 0.15f, 0.20f, 0.9f);

            var medalText = CreateText("Medal", go.transform, TextAlignmentOptions.Top, 46, badgeColour,
                                       FontStyles.Bold | FontStyles.UpperCase);
            var mrt = medalText.rectTransform;
            mrt.anchorMin = new Vector2(0, 1); mrt.anchorMax = new Vector2(1, 1);
            mrt.pivot = new Vector2(0.5f, 1f);
            mrt.offsetMin = new Vector2(0, -70); mrt.offsetMax = new Vector2(0, -10);
            medalText.text = medal;

            _slotName[idx] = CreateText("Name", go.transform, TextAlignmentOptions.Center, 36, JCJUiColors.PodiumBody,
                                        FontStyles.Bold);
            var nrt = _slotName[idx].rectTransform;
            nrt.anchorMin = new Vector2(0, 0.4f); nrt.anchorMax = new Vector2(1, 0.7f);
            nrt.offsetMin = Vector2.zero; nrt.offsetMax = Vector2.zero;
            _slotName[idx].text = "—";

            _slotScore[idx] = CreateText("Score", go.transform, TextAlignmentOptions.Center, 48, badgeColour,
                                         FontStyles.Bold);
            var srt = _slotScore[idx].rectTransform;
            srt.anchorMin = new Vector2(0, 0.1f); srt.anchorMax = new Vector2(1, 0.38f);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            _slotScore[idx].text = "+0";
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, TextAlignmentOptions align, float size, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.alignment = align;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.font = Resources.Load<TMP_FontAsset>("Fonts/Paperlogy-3Light SDF");
            return t;
        }

        private Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas (Podium)");
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

    }
}
