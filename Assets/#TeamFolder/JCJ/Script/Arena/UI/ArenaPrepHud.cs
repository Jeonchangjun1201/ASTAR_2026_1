using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Script.Arena
{
    public class ArenaPrepHud : MonoBehaviour
    {
        private struct NodeVisualLayout
        {
            public ArenaNodeId NodeId;
            public Vector2 Position;

            public NodeVisualLayout(ArenaNodeId nodeId, Vector2 position)
            {
                NodeId = nodeId;
                Position = position;
            }
        }

        private readonly Dictionary<ArenaNodeId, ArenaSkillButton> _buttons = new();
        private readonly List<Image> _connectionLines = new();
        private readonly List<Text> _playerRowTexts = new();
        private readonly List<Image> _playerRowChips = new();
        private readonly List<Text> _playerRowStateTexts = new();
        private Text _phaseText;
        private Text _timerText;
        private Text _modeText;
        private Text _scoreText;
        private Text _statusText;
        private Text _tooltipTitleText;
        private Text _tooltipText;
        private Button _readyButton;
        private Text _readyButtonText;
        private CanvasGroup _rootGroup;
        private Font _font;
        private RectTransform _boardRoot;
        private RectTransform _boardStage;

        private void Awake()
        {
            Build();
        }

        private void Start()
        {
            if (ArenaGameManager.Instance == null)
            {
                return;
            }

            ArenaGameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            ArenaGameManager.Instance.OnPhaseTimerChanged += HandleTimerChanged;
            ArenaGameManager.Instance.OnModeChanged += HandleModeChanged;
            ArenaGameManager.Instance.OnSessionsChanged += Refresh;
            ArenaGameManager.Instance.OnTooltipRequested += ShowTooltip;
            Refresh();
        }

        private void OnDestroy()
        {
            if (ArenaGameManager.Instance == null)
            {
                return;
            }

            ArenaGameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            ArenaGameManager.Instance.OnPhaseTimerChanged -= HandleTimerChanged;
            ArenaGameManager.Instance.OnModeChanged -= HandleModeChanged;
            ArenaGameManager.Instance.OnSessionsChanged -= Refresh;
            ArenaGameManager.Instance.OnTooltipRequested -= ShowTooltip;
        }

        public void TryPurchaseNode(ArenaNodeId nodeId)
        {
            var manager = ArenaGameManager.Instance;
            if (manager == null)
            {
                return;
            }

            var localSession = manager.GetLocalSession();
            if (localSession == null)
            {
                return;
            }

            if (manager.TryPurchaseNode(localSession.PlayerId, nodeId, out string message))
            {
                _statusText.text = message;
            }
            else
            {
                _statusText.text = message;
            }

            Refresh();
        }

        public void ShowTooltip(string tooltip)
        {
            if (_tooltipText != null)
            {
                _tooltipText.text = tooltip;
            }
        }

        public void HideTooltip()
        {
            if (_tooltipText != null)
            {
                _tooltipText.text = "노드를 올리면 설명이 나옵니다.";
            }
        }

        private void Build()
        {
            EnsureEventSystem();
            _font = LoadFont();
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("ArenaCanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var root = CreatePanel("ArenaPrepHud", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, new Color(0.01f, 0.02f, 0.05f, 0.42f));
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            _rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            content.SetParent(root, false);
            SetRect(content, Vector2.zero, Vector2.one, new Vector2(24f, 24f), new Vector2(-24f, -24f), new Vector2(0.5f, 0.5f));
            var contentLayout = content.GetComponent<HorizontalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = true;
            contentLayout.spacing = 20f;

            _boardRoot = CreateStyledPanel("SkillTreePanel", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var boardRootLayout = _boardRoot.gameObject.AddComponent<LayoutElement>();
            boardRootLayout.flexibleWidth = 2.2f;
            boardRootLayout.minWidth = 920f;
            var boardLayout = _boardRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            boardLayout.childAlignment = TextAnchor.UpperCenter;
            boardLayout.childControlWidth = true;
            boardLayout.childControlHeight = true;
            boardLayout.childForceExpandWidth = true;
            boardLayout.childForceExpandHeight = false;
            boardLayout.padding = new RectOffset(18, 18, 18, 18);
            boardLayout.spacing = 16f;

            var boardTitle = CreateLabel("BoardTitle", _boardRoot, "스킬 트리", 34);
            boardTitle.alignment = TextAnchor.MiddleCenter;
            boardTitle.color = JCJUiColors.HudAccent;
            var boardTitleRect = boardTitle.rectTransform;
            boardTitleRect.anchorMin = Vector2.zero;
            boardTitleRect.anchorMax = Vector2.one;
            boardTitleRect.offsetMin = Vector2.zero;
            boardTitleRect.offsetMax = Vector2.zero;
            var boardTitleLayout = boardTitle.gameObject.AddComponent<LayoutElement>();
            boardTitleLayout.preferredHeight = 42f;
            boardTitleLayout.flexibleHeight = 0f;

            _boardStage = new GameObject("BoardStage", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            _boardStage.SetParent(_boardRoot, false);
            _boardStage.anchorMin = Vector2.zero;
            _boardStage.anchorMax = Vector2.one;
            _boardStage.offsetMin = Vector2.zero;
            _boardStage.offsetMax = Vector2.zero;
            var boardImage = _boardStage.GetComponent<Image>();
            boardImage.sprite = _TeamFolder.JCJ.Script.SettingsUiBuilder.GetRoundedSprite();
            boardImage.type = Image.Type.Sliced;
            boardImage.color = new Color(0.11f, 0.09f, 0.06f, 0.96f);
            var boardStageLayout = _boardStage.GetComponent<LayoutElement>();
            boardStageLayout.flexibleHeight = 1f;
            boardStageLayout.minHeight = 620f;

            BuildSkillTreeBoard();

            var right = new GameObject("InfoColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
            right.SetParent(content, false);
            right.anchorMin = Vector2.zero;
            right.anchorMax = Vector2.one;
            right.offsetMin = Vector2.zero;
            right.offsetMax = Vector2.zero;
            var rightColumnLayout = right.GetComponent<LayoutElement>();
            rightColumnLayout.flexibleWidth = 1f;
            rightColumnLayout.minWidth = 420f;
            var rightLayout = right.GetComponent<VerticalLayoutGroup>();
            rightLayout.childAlignment = TextAnchor.UpperCenter;
            rightLayout.childControlWidth = true;
            rightLayout.childControlHeight = true;
            rightLayout.childForceExpandWidth = true;
            rightLayout.childForceExpandHeight = false;
            rightLayout.spacing = 18f;

            var playersCard = CreateInfoCard("플레이어 상태", right, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var playersLayout = playersCard.gameObject.AddComponent<LayoutElement>();
            playersLayout.preferredHeight = 258f;
            playersLayout.flexibleHeight = 1f;
            BuildPlayerRows(playersCard);

            var tooltipCard = CreateStyledPanel("TooltipCard", right, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var tooltipLayout = tooltipCard.gameObject.AddComponent<LayoutElement>();
            tooltipLayout.preferredHeight = 240f;
            tooltipLayout.flexibleHeight = 1f;
            _tooltipText = CreateLabel("Tooltip", tooltipCard, "노드를 올리면 설명이 나옵니다.", 22);
            _tooltipText.alignment = TextAnchor.UpperLeft;
            _tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
            _statusText = CreateLabel("Status", tooltipCard, "노드 선택 후 즉시 구매됩니다.", 20);
            _statusText.color = JCJUiColors.HudMutedText;
            _statusText.alignment = TextAnchor.LowerLeft;
            SetRect(_tooltipText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 40f), new Vector2(-12f, -16f), new Vector2(0.5f, 0.5f));
            SetRect(_statusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 8f), new Vector2(-12f, 32f), new Vector2(0.5f, 0f));

            var actionCard = CreateStyledPanel("ReadyCard", right, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var actionLayout = actionCard.gameObject.AddComponent<LayoutElement>();
            actionLayout.preferredHeight = 150f;
            actionLayout.flexibleHeight = 0f;

            var statusRow = new GameObject("StatusRow", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            statusRow.SetParent(actionCard, false);
            SetRect(statusRow, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -52f), new Vector2(-12f, -8f), new Vector2(0.5f, 1f));
            var statusRowLayout = statusRow.GetComponent<HorizontalLayoutGroup>();
            statusRowLayout.childAlignment = TextAnchor.MiddleCenter;
            statusRowLayout.childControlWidth = true;
            statusRowLayout.childControlHeight = true;
            statusRowLayout.childForceExpandWidth = true;
            statusRowLayout.childForceExpandHeight = true;
            statusRowLayout.spacing = 8f;

            _timerText = CreateStatChip("TimerChip", statusRow, "타이머 60s", 16);
            _modeText = CreateStatChip("ModeChip", statusRow, "모드", 16);
            _scoreText = CreateStatChip("ScoreChip", statusRow, "점수 0", 16);
            _phaseText = CreateLabel("Phase", actionCard, "준비", 1);
            _phaseText.gameObject.SetActive(false);

            var readyButtonObject = new GameObject("ReadyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            readyButtonObject.transform.SetParent(actionCard, false);
            readyButtonObject.GetComponent<Image>().color = new Color(0.24f, 0.40f, 0.70f, 0.96f);
            var buttonRect = readyButtonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.offsetMin = new Vector2(14f, 14f);
            buttonRect.offsetMax = new Vector2(-14f, 78f);
            _readyButton = readyButtonObject.GetComponent<Button>();
            _readyButton.onClick.AddListener(ToggleReady);
            _readyButtonText = CreateLabel("ReadyLabel", readyButtonObject.transform, "준비 완료", 28);
            _readyButtonText.alignment = TextAnchor.MiddleCenter;
            HudTweenHelpers.ButtonHover(_readyButton, readyButtonObject.transform);
            root.localScale = Vector3.one;
        }

        public Font ResolveFont()
        {
            return _font;
        }

        private void BuildSkillTreeBoard()
        {
            var center = CreateStaticNode("StartNode", _boardStage, new Vector2(0f, -20f), 74f, new Color(0.96f, 0.90f, 0.45f, 1f), "시작");
            center.transform.localScale = Vector3.one;

            var layouts = new[]
            {
                new NodeVisualLayout(ArenaNodeId.StrengthTrainingI, new Vector2(-180f, 70f)),
                new NodeVisualLayout(ArenaNodeId.CarryHandling, new Vector2(-330f, 118f)),
                new NodeVisualLayout(ArenaNodeId.HeavyThrow, new Vector2(-485f, 170f)),

                new NodeVisualLayout(ArenaNodeId.JumpBoostI, new Vector2(170f, 60f)),
                new NodeVisualLayout(ArenaNodeId.AirControl, new Vector2(330f, 106f)),
                new NodeVisualLayout(ArenaNodeId.DoubleJump, new Vector2(500f, 162f)),

                new NodeVisualLayout(ArenaNodeId.HealthBoostI, new Vector2(-180f, -88f)),
                new NodeVisualLayout(ArenaNodeId.DamageReduction, new Vector2(-330f, -156f)),
                new NodeVisualLayout(ArenaNodeId.LastStand, new Vector2(-500f, -230f)),

                new NodeVisualLayout(ArenaNodeId.TempoI, new Vector2(170f, -76f)),
                new NodeVisualLayout(ArenaNodeId.ChargePrep, new Vector2(330f, -136f)),
                new NodeVisualLayout(ArenaNodeId.BreathII, new Vector2(495f, -205f))
            };

            for (int i = 0; i < layouts.Length; i++)
            {
                CreateNode(layouts[i]);
            }

            BuildConnections(center.GetComponent<RectTransform>(), layouts);
        }

        private void BuildConnections(RectTransform center, NodeVisualLayout[] layouts)
        {
            var map = new Dictionary<ArenaNodeId, RectTransform>();
            foreach (var pair in _buttons)
            {
                map[pair.Key] = pair.Value.GetComponent<RectTransform>();
            }

            Connect(center.anchoredPosition, map[ArenaNodeId.StrengthTrainingI].anchoredPosition);
            Connect(center.anchoredPosition, map[ArenaNodeId.JumpBoostI].anchoredPosition);
            Connect(center.anchoredPosition, map[ArenaNodeId.HealthBoostI].anchoredPosition);
            Connect(center.anchoredPosition, map[ArenaNodeId.TempoI].anchoredPosition);

            Connect(map[ArenaNodeId.StrengthTrainingI].anchoredPosition, map[ArenaNodeId.CarryHandling].anchoredPosition);
            Connect(map[ArenaNodeId.CarryHandling].anchoredPosition, map[ArenaNodeId.HeavyThrow].anchoredPosition);

            Connect(map[ArenaNodeId.JumpBoostI].anchoredPosition, map[ArenaNodeId.AirControl].anchoredPosition);
            Connect(map[ArenaNodeId.AirControl].anchoredPosition, map[ArenaNodeId.DoubleJump].anchoredPosition);

            Connect(map[ArenaNodeId.HealthBoostI].anchoredPosition, map[ArenaNodeId.DamageReduction].anchoredPosition);
            Connect(map[ArenaNodeId.DamageReduction].anchoredPosition, map[ArenaNodeId.LastStand].anchoredPosition);

            Connect(map[ArenaNodeId.TempoI].anchoredPosition, map[ArenaNodeId.ChargePrep].anchoredPosition);
            Connect(map[ArenaNodeId.ChargePrep].anchoredPosition, map[ArenaNodeId.BreathII].anchoredPosition);
        }

        private void CreateNode(NodeVisualLayout layout)
        {
            var nodeObject = new GameObject(layout.NodeId.ToString(), typeof(RectTransform), typeof(Image), typeof(Button), typeof(ArenaSkillButton));
            nodeObject.transform.SetParent(_boardStage, false);
            var rect = nodeObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = layout.Position;
            rect.sizeDelta = new Vector2(150f, 100f);
            nodeObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var skillButton = nodeObject.GetComponent<ArenaSkillButton>();
            skillButton.Setup(this, ArenaSkillCatalog.Get(layout.NodeId));
            _buttons[layout.NodeId] = skillButton;
        }

        private void Connect(Vector2 from, Vector2 to)
        {
            var line = CreateBoardStroke(from, to, 6f, new Color(0.92f, 0.76f, 0.24f, 0.78f));
            _connectionLines.Add(line);
        }

        private Image CreateBoardStroke(Vector2 from, Vector2 to, float thickness, Color color)
        {
            var go = new GameObject("Connection", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_boardStage, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            Vector2 delta = to - from;
            rect.sizeDelta = new Vector2(delta.magnitude, thickness);
            rect.anchoredPosition = from;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var image = go.GetComponent<Image>();
            image.sprite = _TeamFolder.JCJ.Script.SettingsUiBuilder.GetRoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            go.transform.SetAsFirstSibling();
            return image;
        }

        private RectTransform CreateStyledPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = CreatePanel(name, parent, anchorMin, anchorMax, Vector2.zero, JCJUiColors.HudPanel);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = rect.GetComponent<Image>();
            image.sprite = _TeamFolder.JCJ.Script.SettingsUiBuilder.GetRoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = JCJUiColors.HudPanel;
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = JCJUiColors.HudShadow;
            shadow.effectDistance = new Vector2(8f, -8f);
            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(rect, false);
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.offsetMin = new Vector2(16f, -5f);
            accentRect.offsetMax = new Vector2(-16f, 0f);
            var accentImage = accent.GetComponent<Image>();
            accentImage.color = JCJUiColors.HudAccentLine;
            accentImage.sprite = _TeamFolder.JCJ.Script.SettingsUiBuilder.GetRoundedSprite();
            accentImage.type = Image.Type.Sliced;
            return rect;
        }

        private GameObject CreateStaticNode(string name, Transform parent, Vector2 position, float size, Color color, string label)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size + 32f);

            var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(node.transform, false);
            var ringRect = ring.GetComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.anchoredPosition = Vector2.zero;
            ringRect.sizeDelta = new Vector2(size, size);
            ringRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var ringImage = ring.GetComponent<Image>();
            ringImage.sprite = _TeamFolder.JCJ.Script.SettingsUiBuilder.GetRoundedSprite();
            ringImage.type = Image.Type.Sliced;
            ringImage.color = new Color(1.00f, 0.92f, 0.55f, 0.96f);

            var core = new GameObject("Core", typeof(RectTransform), typeof(Image));
            core.transform.SetParent(node.transform, false);
            var coreRect = core.GetComponent<RectTransform>();
            coreRect.anchorMin = new Vector2(0.5f, 0.5f);
            coreRect.anchorMax = new Vector2(0.5f, 0.5f);
            coreRect.pivot = new Vector2(0.5f, 0.5f);
            coreRect.anchoredPosition = Vector2.zero;
            coreRect.sizeDelta = new Vector2(size - 18f, size - 18f);
            coreRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var coreImage = core.GetComponent<Image>();
            coreImage.sprite = _TeamFolder.JCJ.Script.SettingsUiBuilder.GetRoundedSprite();
            coreImage.type = Image.Type.Sliced;
            coreImage.color = color;

            var text = CreateLabel("Label", node.transform, label, 18);
            text.alignment = TextAnchor.UpperCenter;
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0f);
            textRect.anchorMax = new Vector2(0.5f, 0f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0f, -18f);
            textRect.sizeDelta = new Vector2(120f, 30f);
            return node;
        }

        private void ToggleReady()
        {
            if (ArenaGameManager.Instance == null)
            {
                return;
            }

            if (ArenaGameManager.Instance.CurrentPhase == ArenaPhase.Finished)
            {
                ArenaGameManager.Instance.BeginMinigame();
                Refresh();
                return;
            }

            var local = ArenaGameManager.Instance.GetLocalSession();
            if (local == null)
            {
                return;
            }

            ArenaGameManager.Instance.SetPlayerReady(local.PlayerId, !local.IsReady);
            Refresh();
        }

        private void HandlePhaseChanged(ArenaPhase phase)
        {
            _phaseText.text = phase switch
            {
                ArenaPhase.Preparation => "준비",
                ArenaPhase.Playing => "전투",
                ArenaPhase.Finished => "결과",
                _ => "대기"
            };

            bool showFullHud = phase == ArenaPhase.Preparation;
            _rootGroup.alpha = phase == ArenaPhase.Inactive ? 0f : 1f;
            if (_rootGroup != null)
            {
                _rootGroup.blocksRaycasts = showFullHud;
                _rootGroup.interactable = showFullHud;
                _rootGroup.DOKill();
                _rootGroup.DOFade(showFullHud ? 1f : 0f, 0.22f).SetEase(Ease.OutQuad);
            }
        }

        private void HandleTimerChanged(float remaining)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            _timerText.text = $"타이머 {seconds}s";
        }

        private void HandleModeChanged(ArenaModeType modeType)
        {
            _modeText.text = modeType == ArenaModeType.Team2v2 ? "모드 2:2" : "모드 1:1:1:1";
        }

        private void Refresh()
        {
            var manager = ArenaGameManager.Instance;
            if (manager == null)
            {
                return;
            }

            var local = manager.GetLocalSession();
            if (local != null)
            {
                _scoreText.text = $"점수 {local.StoredScore}";
                _readyButtonText.text = manager.CurrentPhase == ArenaPhase.Finished
                    ? "다음 전투 시작"
                    : local.IsReady ? "준비 해제" : "준비 완료";
            }

            RefreshPlayerRows(manager.Sessions);
            RefreshButtons(local);
        }

        private void RefreshButtons(ArenaPlayerSessionState local)
        {
            foreach (var pair in _buttons)
            {
                var definition = ArenaSkillCatalog.Get(pair.Key);
                if (local == null)
                {
                    pair.Value.Refresh(string.Empty, false, false);
                    continue;
                }

                bool purchased = local.HasNode(pair.Key);
                bool interactable = false;
                string status = string.Empty;
                if (purchased)
                {
                    status = "구매 완료";
                }
                else if (ArenaSkillCatalog.CanPurchase(local, pair.Key, out string failureReason))
                {
                    interactable = ArenaGameManager.Instance.CurrentPhase == ArenaPhase.Preparation;
                    status = "구매 가능";
                }
                else
                {
                    status = failureReason;
                }

                pair.Value.Refresh(status, interactable, purchased);
            }
        }

        private void RefreshPlayerRows(IReadOnlyList<ArenaPlayerSessionState> sessions)
        {
            for (int i = 0; i < _playerRowTexts.Count; i++)
            {
                if (i >= sessions.Count)
                {
                    _playerRowTexts[i].text = string.Empty;
                    _playerRowStateTexts[i].text = string.Empty;
                    continue;
                }

                var session = sessions[i];
                _playerRowTexts[i].text = $"{session.DisplayName}   남은 점수 {session.StoredScore}";
                if (ArenaGameManager.Instance != null && ArenaGameManager.Instance.CurrentPhase == ArenaPhase.Finished)
                {
                    _playerRowStateTexts[i].text = session.Placement > 0 ? $"{session.Placement}위" : "-";
                    _playerRowChips[i].color = session.Placement == 1
                        ? new Color(0.92f, 0.78f, 0.32f, 0.98f)
                        : session.Placement == 2
                            ? new Color(0.72f, 0.76f, 0.84f, 0.98f)
                            : new Color(0.56f, 0.60f, 0.68f, 0.98f);
                    _playerRowStateTexts[i].color = JCJUiColors.HudPrimaryText;
                }
                else
                {
                    _playerRowStateTexts[i].text = session.IsReady ? "READY" : "WAIT";
                    _playerRowChips[i].color = session.IsReady
                        ? new Color(0.30f, 0.78f, 0.52f, 0.98f)
                        : new Color(0.56f, 0.60f, 0.68f, 0.98f);
                    _playerRowStateTexts[i].color = session.IsReady
                        ? new Color(0.70f, 0.96f, 0.76f, 1f)
                        : JCJUiColors.HudMutedText;
                }
            }
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            gameObject.GetComponent<Image>().color = color;
            return rect;
        }

        private Text CreateLabel(string name, Transform parent, string text, int fontSize)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6f, 6f);
            rect.offsetMax = new Vector2(-6f, -6f);
            var label = gameObject.GetComponent<Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = new Color(0.96f, 0.97f, 0.99f, 1f);
            label.alignment = TextAnchor.MiddleLeft;
            label.supportRichText = true;
            if (_font != null)
            {
                label.font = _font;
            }
            else
            {
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return label;
        }

        private RectTransform CreateInfoCard(string title, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var card = CreateStyledPanel(title + "Card", parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var titleText = CreateLabel(title + "Title", card, title, 22);
            titleText.color = JCJUiColors.HudAccent;
            titleText.alignment = TextAnchor.MiddleLeft;
            SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -44f), new Vector2(-12f, -8f), new Vector2(0.5f, 1f));
            return card;
        }

        private Text CreateStatChip(string name, Transform parent, string text, int fontSize)
        {
            var chip = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            chip.transform.SetParent(parent, false);
            var chipRect = chip.GetComponent<RectTransform>();
            chipRect.anchorMin = Vector2.zero;
            chipRect.anchorMax = Vector2.one;
            chipRect.offsetMin = Vector2.zero;
            chipRect.offsetMax = Vector2.zero;
            var chipImage = chip.GetComponent<Image>();
            chipImage.sprite = _TeamFolder.JCJ.Script.SettingsUiBuilder.GetRoundedSprite();
            chipImage.type = Image.Type.Sliced;
            chipImage.color = new Color(0.12f, 0.15f, 0.20f, 0.96f);
            var chipLayout = chip.GetComponent<LayoutElement>();
            chipLayout.flexibleWidth = 1f;
            chipLayout.preferredHeight = 40f;
            var label = CreateLabel(name + "Text", chip.transform, text, fontSize);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = JCJUiColors.HudPrimaryText;
            return label;
        }

        private void BuildPlayerRows(RectTransform parent)
        {
            for (int i = 0; i < 4; i++)
            {
                var row = new GameObject("PlayerRow_" + (i + 1), typeof(RectTransform), typeof(Image));
                row.transform.SetParent(parent, false);
                var rowRect = row.GetComponent<RectTransform>();
                SetRect(rowRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -68f - (i * 46f)), new Vector2(-12f, -106f - (i * 46f)), new Vector2(0.5f, 1f));
                var rowImage = row.GetComponent<Image>();
                rowImage.sprite = _TeamFolder.JCJ.Script.SettingsUiBuilder.GetRoundedSprite();
                rowImage.type = Image.Type.Sliced;
                rowImage.color = new Color(0.12f, 0.14f, 0.20f, 0.94f);

                var chip = new GameObject("Chip", typeof(RectTransform), typeof(Image));
                chip.transform.SetParent(row.transform, false);
                var chipRect = chip.GetComponent<RectTransform>();
                SetRect(chipRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, -9f), new Vector2(28f, 9f), new Vector2(0f, 0.5f));
                var chipImage = chip.GetComponent<Image>();
                chipImage.sprite = _TeamFolder.JCJ.Script.SettingsUiBuilder.GetRoundedSprite();
                chipImage.type = Image.Type.Sliced;
                chipImage.color = new Color(0.56f, 0.60f, 0.68f, 0.98f);
                _playerRowChips.Add(chipImage);

                var label = CreateLabel("PlayerText", row.transform, string.Empty, 17);
                label.alignment = TextAnchor.MiddleLeft;
                SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(36f, 4f), new Vector2(-92f, -4f), new Vector2(0.5f, 0.5f));
                _playerRowTexts.Add(label);

                var stateText = CreateLabel("StateText", row.transform, "WAIT", 15);
                stateText.alignment = TextAnchor.MiddleRight;
                stateText.fontStyle = FontStyle.Bold;
                SetRect(stateText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-86f, 4f), new Vector2(-10f, -4f), new Vector2(1f, 0.5f));
                _playerRowStateTexts.Add(stateText);
            }
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.pivot = pivot;
        }

        private Font LoadFont()
        {
            Font font = Resources.Load<Font>("Fonts/malgun");
#if UNITY_EDITOR
            if (font == null)
            {
                font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/#TeamFolder/JCJ/Resources/Fonts/malgun.ttf");
            }
#endif
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 16);
            }
            return font;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(null, false);
        }
    }
}
