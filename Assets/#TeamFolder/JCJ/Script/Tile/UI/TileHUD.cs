using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using _TeamFolder.JCJ.Script;

// 타일 미니게임 중 표시되는 HUD를 제어하는 UI 컴포넌트.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 타일 미니게임용 자체 생성 HUD. TileGameManager가 public API로 호출 — 씬 배선 최소.
    /// 좌상 생존·목숨, 상단 큰 타이머, 우상 컬러콜 배너, 중앙 카운트다운, 종료 시 순위+다시하기 오버레이.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class TileHUD : MonoBehaviour
    {
        private Canvas             _canvas;
        private CanvasGroup        _root;

        // 상시 위젯.
        private TextMeshProUGUI    _timerText;
        private TextMeshProUGUI    _aliveText;
        private TextMeshProUGUI    _livesText;
        private TextMeshProUGUI    _countdownText;
        private RectTransform      _countdownRect;
        private TextMeshProUGUI    _colorCallText;
        private CanvasGroup        _colorCallGroup;
        private Image              _colorCallBar;
        private RectTransform      _colorCallBarRoot;

        // 결과 오버레이(첫 Finished 때 지연 생성).
        private CanvasGroup        _resultGroup;
        private TextMeshProUGUI    _resultBody;

        // 코루틴 핸들 — StopAllCoroutines 대신 필요한 것만 중지(이 컴포넌트의 다른 UI 트윈 유지).
        private Coroutine          _countdownCo;
        private Coroutine          _colorCallBarCo;
        private Coroutine          _colorCallFadeCo;
        private Coroutine          _resultFadeCo;

        private void Awake()
        {
            BuildCanvas();
            BuildTopBar();
            BuildCountdown();
            BuildColorCallBanner();
            BuildLegend();
        }

        // ── 공개 API(TileGameManager 호출) ───────────
        public void SetTimer(float seconds)
        {
            if (_timerText == null) return;
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int m = s / 60;
            int r = s % 60;
            _timerText.text = m > 0 ? $"{m}:{r:00}" : $"{r}";
            _timerText.color = seconds <= 10f ? JCJUiColors.HudDangerSoft : JCJUiColors.HudAccent;
        }

        public void SetAlive(int alive, int total)
        {
            if (_aliveText == null) return;
            _aliveText.text = $"ALIVE  {alive}/{total}";
        }

        public void SetLives(int lives, int max)
        {
            if (_livesText == null) return;
            var sb = new System.Text.StringBuilder();
            sb.Append("LIVES  ");
            for (int i = 0; i < max; i++) sb.Append(i < lives ? '#' : '-');
            _livesText.text = sb.ToString();
        }

        public void ShowCountdown(string label)
        {
            if (_countdownText == null) return;
            _countdownText.gameObject.SetActive(true);
            _countdownText.text = label;
            _countdownRect.localScale = Vector3.one * 1.6f;
            if (_countdownCo != null) StopCoroutine(_countdownCo);
            _countdownCo = StartCoroutine(CountdownPop());
        }

        public void HideCountdown()
        {
            if (_countdownText != null) _countdownText.gameObject.SetActive(false);
        }

        private IEnumerator CountdownPop()
        {
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.unscaledDeltaTime;
                float e = 1f - Mathf.Pow(1f - t / 0.22f, 3f);
                _countdownRect.localScale = Vector3.Lerp(Vector3.one * 1.6f, Vector3.one, e);
                yield return null;
            }
        }

        // 안전색 공지를 HUD에 띄우는 진입점이다.
        // 서버가 안전색을 결정하더라도 클라이언트 UI 반영은 이 메서드로 충분하다.
        public void ShowColorCallAnnounce(TileColor color, float duration)
        {
            if (_colorCallGroup == null) return;
            _colorCallText.text = $"COLOR CALL  —  {color.ToString().ToUpper()} ONLY";
            _colorCallText.color = TileColorToDisplay(color);
            _colorCallGroup.alpha = 1f;
            // 문자열로 시작한 코루틴만 nameof 중지 가능 — 핸들로 잡아 바 트윈을 확실히 취소.
            if (_colorCallBarCo != null) StopCoroutine(_colorCallBarCo);
            _colorCallBarCo = StartCoroutine(ColorCallBarRoutine(duration));
        }

        public void HideColorCall()
        {
            if (_colorCallGroup == null) return;
            if (_colorCallBarCo != null) StopCoroutine(_colorCallBarCo);
            if (_colorCallFadeCo != null) StopCoroutine(_colorCallFadeCo);
            _colorCallFadeCo = StartCoroutine(FadeGroup(_colorCallGroup, 1f, 0f, 0.35f));
        }

        private IEnumerator ColorCallBarRoutine(float duration)
        {
            if (_colorCallBar == null) yield break;
            _colorCallBar.fillAmount = 1f;
            float t = 0f;
            while (t < duration)
            {
                // timeScale이 내려가도 바가 줄어들게 unscaled 시간 사용(다른 HUD 트윈과 동일).
                t += Time.unscaledDeltaTime;
                _colorCallBar.fillAmount = 1f - Mathf.Clamp01(t / duration);
                yield return null;
            }
            _colorCallBar.fillAmount = 0f;
            _colorCallBarCo = null;
        }

        private IEnumerator FadeGroup(CanvasGroup g, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            g.alpha = to;
        }

        // ── 결과 오버레이 ───────────────────────────
        // 라운드 종료 결과와 재시작 버튼을 띄우는 UI 진입점이다.
        // 서버 매치에서는 버튼이 직접 다시 시작하지 않도록 onRestart 연결 방식만 바꾸면 된다.
        public void ShowResults(IReadOnlyList<string> rankingLines, System.Action onRestart)
        {
            if (_resultGroup == null) BuildResultOverlay(onRestart);
            _resultGroup.gameObject.SetActive(true);
            _resultGroup.alpha = 0f;
            _resultGroup.interactable = true;
            _resultGroup.blocksRaycasts = true;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < rankingLines.Count; i++)
                sb.AppendLine(rankingLines[i]);
            _resultBody.text = sb.ToString();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            if (_resultFadeCo != null) StopCoroutine(_resultFadeCo);
            _resultFadeCo = StartCoroutine(FadeGroup(_resultGroup, 0f, 1f, 0.45f));
        }

        public void HideResults()
        {
            if (_resultGroup == null) return;
            _resultGroup.alpha = 0f;
            _resultGroup.interactable = false;
            _resultGroup.blocksRaycasts = false;
            _resultGroup.gameObject.SetActive(false);
        }

        // ── UI 생성 헬퍼 ────────────────────────────
        private void BuildCanvas()
        {
            var canvasGO = new GameObject("TileHUDCanvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem 없으면 버튼 클릭 불가 — 없으면 새 Input System 모듈로 자동 생성.
            EnsureEventSystem();

            _root = canvasGO.AddComponent<CanvasGroup>();
            _root.alpha = 1f;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem (auto)");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildTopBar()
        {
            // 타이머 — 글래스 판 + 큰 숫자
            var timerPlate = CreateGlassPanel("TimerPlate", _canvas.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -32f), new Vector2(340f, 104f));
            var timer = NewText("Timer", timerPlate.transform, 88, TextAlignmentOptions.Center, JCJUiColors.HudAccent);
            SetAnchors(timer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _timerText = timer;
            _timerText.fontStyle = FontStyles.Bold;
            _timerText.characterSpacing = 3f;
            ApplyOutline(_timerText, 0.2f);

            // 생존·목숨 — 한 장의 글래스 카드
            var statusPlate = CreateGlassPanel("StatusPlate", _canvas.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(132f, -36f), new Vector2(300f, 108f));
            var alive = NewText("Alive", statusPlate.transform, 26, TextAlignmentOptions.Left, JCJUiColors.HudAccent);
            SetAnchors(alive.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
                        new Vector2(18f, -8f), new Vector2(-16f, -10f));
            ApplyOutline(alive, 0.12f);
            _aliveText = alive;

            var lives = NewText("Lives", statusPlate.transform, 24, TextAlignmentOptions.Left, JCJUiColors.HudMutedText);
            SetAnchors(lives.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                        new Vector2(18f, 8f), new Vector2(-16f, -6f));
            _livesText = lives;
        }

        private void BuildCountdown()
        {
            var txt = NewText("Countdown", _canvas.transform, 200, TextAlignmentOptions.Center, JCJUiColors.HudAccent);
            SetAnchors(txt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, 40f), new Vector2(640f, 300f));
            txt.fontStyle = FontStyles.Bold;
            ApplyOutline(txt, 0.28f);
            txt.gameObject.SetActive(false);
            _countdownText = txt;
            _countdownRect = txt.rectTransform;
        }

        private void BuildColorCallBanner()
        {
            var go = new GameObject("ColorCallBanner");
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            SetAnchors(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -236f), new Vector2(780f, 128f));
            _colorCallGroup = go.AddComponent<CanvasGroup>();
            _colorCallGroup.alpha = 0f;
            _colorCallGroup.blocksRaycasts = false;

            var bg = new GameObject("BG").AddComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            bg.color = JCJUiColors.HudPanel;
            var bannerShadow = bg.gameObject.AddComponent<Shadow>();
            bannerShadow.effectColor = JCJUiColors.HudShadow;
            bannerShadow.effectDistance = new Vector2(5f, -5f);

            // BG 다음에 그려 헤어라인이 채움 위에 보이게.
            AddAccentLine(go.transform);

            var txt = NewText("Text", go.transform, 48, TextAlignmentOptions.Center, JCJUiColors.HudAccent);
            SetAnchors(txt.rectTransform, new Vector2(0f, 0.25f), new Vector2(1f, 1f),
                        new Vector2(0f, 0f), new Vector2(0f, 0f));
            txt.fontStyle = FontStyles.Bold;
            ApplyOutline(txt, 0.16f);
            _colorCallText = txt;

            var barRoot = new GameObject("BarRoot").AddComponent<RectTransform>();
            barRoot.SetParent(go.transform, false);
            SetAnchors(barRoot, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.22f),
                        new Vector2(0f, 0f), new Vector2(0f, 0f));
            _colorCallBarRoot = barRoot;
            var barBG = new GameObject("BarBG").AddComponent<Image>();
            barBG.transform.SetParent(barRoot, false);
            var bbgRT = barBG.GetComponent<RectTransform>();
            bbgRT.anchorMin = Vector2.zero; bbgRT.anchorMax = Vector2.one;
            bbgRT.offsetMin = Vector2.zero; bbgRT.offsetMax = Vector2.zero;
            barBG.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);

            var bar = new GameObject("Bar").AddComponent<Image>();
            bar.transform.SetParent(barRoot, false);
            var barRT = bar.GetComponent<RectTransform>();
            barRT.anchorMin = Vector2.zero; barRT.anchorMax = Vector2.one;
            barRT.offsetMin = Vector2.zero; barRT.offsetMax = Vector2.zero;
            bar.color = new Color(0.55f, 0.78f, 1.00f, 1f);
            bar.type = Image.Type.Filled;
            bar.fillMethod = Image.FillMethod.Horizontal;
            bar.fillOrigin = 0;
            bar.fillAmount = 0f;
            _colorCallBar = bar;
        }

        // 좌하단 — 색별 타일 설명(초보용).
        private void BuildLegend()
        {
            var panel = new GameObject("LegendPanel").AddComponent<Image>();
            panel.transform.SetParent(_canvas.transform, false);
            panel.color = JCJUiColors.HudPanel;
            var rt = panel.GetComponent<RectTransform>();
            SetAnchors(rt,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 0f),
                anchoredPos: new Vector2(168f, 268f),
                size: new Vector2(312f, 492f));
            var plateShadow = panel.gameObject.AddComponent<Shadow>();
            plateShadow.effectColor = JCJUiColors.HudShadow;
            plateShadow.effectDistance = new Vector2(6f, -6f);
            AddAccentLine(panel.transform);

            var title = NewText("LegendTitle", panel.transform, 22, TextAlignmentOptions.Center, JCJUiColors.HudAccent);
            SetAnchors(title.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -28f), new Vector2(0f, 44f));
            title.text = "TILE GUIDE";
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 1.5f;
            ApplyOutline(title, 0.12f);

            // 항목: 표시 색 → 라벨 → 짧은 설명
            var entries = new (TileColor color, string name, string desc)[]
            {
                (TileColor.Green,   "NORMAL",  "Just falls"),
                (TileColor.Red,     "BOMB",    "Explodes nearby"),
                (TileColor.Purple,  "WEB",     "Slows you down"),
                (TileColor.Cyan,    "ICE",     "Slippery surface"),
                (TileColor.Orange,  "BALLOON", "Floats up"),
                (TileColor.Lime,    "JUMP",    "Big launch"),
                (TileColor.Magenta, "CONFUSE", "Inverts controls"),
            };

            float rowHeight = 54f;
            for (int i = 0; i < entries.Length; i++)
            {
                var (col, nm, dsc) = entries[i];
                BuildLegendRow(panel.transform, i, rowHeight, col, nm, dsc);
            }
        }

        private void BuildLegendRow(Transform parent, int index, float rowHeight,
                                    TileColor color, string label, string description)
        {
            var row = new GameObject($"Row_{label}").AddComponent<RectTransform>();
            row.SetParent(parent, false);
            // 패널 상단(1,1) 기준으로 제목 영역만큼 아래로 오프셋.
            const float topMargin = 72f;
            SetAnchors(row,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -(topMargin + index * rowHeight)),
                new Vector2(-24f, rowHeight));

            // 왼쪽 색 스와치.
            var swatch = new GameObject("Swatch").AddComponent<Image>();
            swatch.transform.SetParent(row, false);
            swatch.color = TileColorToDisplay(color);
            var swRT = swatch.GetComponent<RectTransform>();
            SetAnchors(swRT,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(22f, 0f), new Vector2(26f, 26f));

            // 텍스트 열 상단 라벨.
            var nameText = NewText("Name", row, 22, TextAlignmentOptions.Left, JCJUiColors.HudAccent);
            nameText.fontStyle = FontStyles.Bold;
            SetAnchors(nameText.rectTransform,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f),
                new Vector2(30f, 0f), new Vector2(-48f, 0f));
            nameText.text = label;
            ApplyOutline(nameText, 0.08f);

            // 하단 설명.
            var descText = NewText("Desc", row, 17, TextAlignmentOptions.Left, JCJUiColors.HudMutedText);
            SetAnchors(descText.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                new Vector2(30f, 0f), new Vector2(-48f, 0f));
            descText.text = description;
        }

        private void BuildResultOverlay(System.Action onRestart)
        {
            var go = new GameObject("ResultOverlay");
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _resultGroup = go.AddComponent<CanvasGroup>();

            var dim = new GameObject("Dim").AddComponent<Image>();
            dim.transform.SetParent(go.transform, false);
            var dimRT = dim.GetComponent<RectTransform>();
            dimRT.anchorMin = Vector2.zero;
            dimRT.anchorMax = Vector2.one;
            dimRT.offsetMin = Vector2.zero;
            dimRT.offsetMax = Vector2.zero;
            dim.color = new Color(0.02f, 0.03f, 0.06f, 0.78f);

            var panel = new GameObject("Panel").AddComponent<Image>();
            panel.transform.SetParent(go.transform, false);
            var panelRT = panel.GetComponent<RectTransform>();
            SetAnchors(panelRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, 0f), new Vector2(800f, 620f));
            panel.color = JCJUiColors.HudPanel;
            var pShadow = panel.gameObject.AddComponent<Shadow>();
            pShadow.effectColor = JCJUiColors.HudShadow;
            pShadow.effectDistance = new Vector2(8f, -8f);
            AddAccentLine(panel.transform);

            var title = NewText("Title", panel.transform, 58, TextAlignmentOptions.Center, JCJUiColors.HudAccent);
            SetAnchors(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                        new Vector2(0f, -76f), new Vector2(0f, 88f));
            title.text = "RESULTS";
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 4f;
            ApplyOutline(title, 0.18f);

            var body = NewText("Body", panel.transform, 36, TextAlignmentOptions.Center, JCJUiColors.HudMutedText);
            SetAnchors(body.rectTransform, new Vector2(0f, 0.18f), new Vector2(1f, 0.82f),
                        new Vector2(0f, 0f), new Vector2(0f, 0f));
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 8f;
            ApplyOutline(body, 0.1f);
            _resultBody = body;

            var btnGO = new GameObject("RestartBtn");
            btnGO.transform.SetParent(panel.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            SetAnchors(btnRT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 72f), new Vector2(340f, 82f));
            var btnBG = btnGO.AddComponent<Image>();
            btnBG.color = new Color(0.88f, 0.92f, 0.99f, 1f);
            var btnSh = btnGO.AddComponent<Shadow>();
            btnSh.effectColor = new Color(0f, 0f, 0f, 0.25f);
            btnSh.effectDistance = new Vector2(3f, -3f);
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnBG;
            btn.onClick.AddListener(() => onRestart?.Invoke());

            var btnTxt = NewText("Label", btnGO.transform, 30, TextAlignmentOptions.Center,
                                  new Color(0.06f, 0.08f, 0.12f));
            SetAnchors(btnTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            btnTxt.text = "PLAY AGAIN";
            btnTxt.fontStyle = FontStyles.Bold | FontStyles.UpperCase;

            _resultGroup.alpha = 0f;
            _resultGroup.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size,
                                                TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = string.Empty;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.enableAutoSizing = false;
            t.rectTransform.localScale = Vector3.one;
            t.enableVertexGradient = false;
            return t;
        }

        private static void ApplyOutline(TextMeshProUGUI t, float width = 0.15f)
        {
            if (t == null) return;
            t.outlineWidth = width;
            t.outlineColor = JCJUiColors.HudTextOutline;
        }

        /// <summary>어두운 글래스 패널 + 그림자 + 상단 얇은 액센트(미로 HUD와 동일 톤).</summary>
        private static Image CreateGlassPanel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;

            var img = go.AddComponent<Image>();
            img.color = JCJUiColors.HudPanel;
            img.raycastTarget = false;

            var sh = go.AddComponent<Shadow>();
            sh.effectColor = JCJUiColors.HudShadow;
            sh.effectDistance = new Vector2(6f, -6f);

            AddAccentLine(go.transform);
            return img;
        }

        private static void AddAccentLine(Transform parent)
        {
            var accent = new GameObject("AccentLine");
            accent.transform.SetParent(parent, false);
            var art = accent.AddComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 1f);
            art.anchorMax = new Vector2(1f, 1f);
            art.pivot = new Vector2(0.5f, 1f);
            art.offsetMin = new Vector2(12f, -3f);
            art.offsetMax = new Vector2(-12f, 0f);
            var aim = accent.AddComponent<Image>();
            aim.color = JCJUiColors.HudAccentLine;
            aim.raycastTarget = false;
        }

        private static void SetAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                                        Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static Color TileColorToDisplay(TileColor c) => c switch
        {
            TileColor.Green   => new Color(0.55f, 1.00f, 0.70f),
            TileColor.Blue    => new Color(0.55f, 0.80f, 1.00f),
            TileColor.Yellow  => new Color(1.00f, 0.95f, 0.50f),
            TileColor.Red     => new Color(1.00f, 0.55f, 0.55f),
            TileColor.Purple  => new Color(0.85f, 0.65f, 1.00f),
            TileColor.Cyan    => new Color(0.55f, 1.00f, 1.00f),
            TileColor.Orange  => new Color(1.00f, 0.75f, 0.45f),
            TileColor.Lime    => new Color(0.75f, 1.00f, 0.55f),
            TileColor.Magenta => new Color(1.00f, 0.65f, 0.90f),
            _                 => JCJUiColors.HudAccent,
        };
    }
}
