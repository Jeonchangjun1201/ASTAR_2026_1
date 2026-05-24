using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using _TeamFolder.JCJ.Battle.Session;

// 설정 패널 전체 열기/닫기와 탭 구성을 관리하는 UI.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 설정 패널에 표시할 탭 구성. BattleMinimal은 DPI·키 바인딩만 노출한다.
    /// </summary>
    public enum SettingsPanelProfile
    {
        Full,
        BattleMinimal,
    }

    /// <summary>
    /// 런타임 설정 창을 생성하고 탭 전환, 저장, 기본값 복원, 커서 잠금을 관리한다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SettingsPanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        [SerializeField] private SettingsPanelProfile _profile = SettingsPanelProfile.Full;
        [SerializeField] private bool _toggleWithEscape = true;
        [SerializeField] private bool _autoOpenOnStart = false;

        private ISettingsService _settings;
        private ICustomizeService _customize;
        private GameObject _root;
        private readonly List<ISettingsTab> _tabs = new();
        private readonly List<GameObject> _tabContents = new();
        private readonly List<Button> _tabButtons = new();
        private RectTransform _contentArea;
        private int _activeIndex;

        private void Start()
        {
            _settings = SettingsService.EnsureInstance();
            EnsureAudioBinder(_settings);
            _customize = CustomizeService.EnsureInstance();
            SettingsUiBuilder.EnsureEventSystem();
            BuildUi();
            _settings.OnChanged += HandleChanged;
            HandleChanged(_settings.Data);
            if (!_autoOpenOnStart) SetVisible(false);
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.OnChanged -= HandleChanged;
            IsOpen = false;
        }

        private void Update()
        {
            if (!_toggleWithEscape) return;
            if (Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                Toggle();
        }

        public void Toggle()
        {
            if (_root == null) return;
            SetVisible(!_root.activeSelf);
        }

        public void SetVisible(bool visible)
        {
            if (_root == null) return;
            IsOpen = visible;
            _root.SetActive(visible);
            if (visible)
            {
                GameplayCursor.SetLocked(false);
            }
            else
            {
                ApplyCursorLockForGameplay();
            }
        }

        private static void ApplyCursorLockForGameplay()
        {
            if (BattleMatchRegistry.TryGetMatch(out _))
            {
                GameplayCursor.SetLocked(true);
                return;
            }

            bool lockCursor = false;

            var maze = GameStateManager.Instance;
            if (maze != null)
            {
                lockCursor = maze.CurrentState == GameState.Playing
                          || maze.CurrentState == GameState.Countdown;
            }

            var tile = _TeamFolder.JCJ.TileGame.TileGameManager.Instance;
            if (tile != null)
            {
                lockCursor = lockCursor
                    || tile.State == GameState.Playing
                    || tile.State == GameState.Countdown;
            }

            GameplayCursor.SetLocked(lockCursor);
        }

        private void BuildUi()
        {
            // 설정 UI는 프리팹 없이 코드로 생성해 Maze/Tile 어느 씬에서도 같은 패널을 재사용한다.
            var canvas = SettingsUiBuilder.EnsureCanvas();
            _root = SettingsUiBuilder.CreatePanel(
                canvas.transform as RectTransform,
                "JCJ_SettingsRoot",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero,
                bg: new Color(0.0f, 0.0f, 0.0f, 0.55f));

            var window = SettingsUiBuilder.CreatePanel(
                _root.GetComponent<RectTransform>(),
                "Window",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-450f, -320f), new Vector2(450f, 320f));
            var windowRt = window.GetComponent<RectTransform>();

            const float SidePad = 20f;
            const float TopPad = 16f;
            const float BottomPad = 16f;
            const float HeaderHeight = 38f;
            const float TabHeight = 36f;
            const float BottomBarHeight = 36f;
            const float Gap = 10f;

            BuildHeader(windowRt, SidePad, TopPad, HeaderHeight);

            var tabBar = new GameObject("TabBar", typeof(RectTransform));
            var tabBarRt = tabBar.GetComponent<RectTransform>();
            tabBarRt.SetParent(window.transform, false);
            tabBarRt.anchorMin = new Vector2(0f, 1f);
            tabBarRt.anchorMax = new Vector2(1f, 1f);
            tabBarRt.pivot = new Vector2(0.5f, 1f);
            float tabTopOffset = TopPad + HeaderHeight + Gap;
            tabBarRt.offsetMin = new Vector2(SidePad, -(tabTopOffset + TabHeight));
            tabBarRt.offsetMax = new Vector2(-SidePad, -tabTopOffset);
            var hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = 6f;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var content = new GameObject("ContentArea", typeof(RectTransform));
            _contentArea = content.GetComponent<RectTransform>();
            _contentArea.SetParent(window.transform, false);
            _contentArea.anchorMin = new Vector2(0f, 0f);
            _contentArea.anchorMax = new Vector2(1f, 1f);
            float contentTopMargin = tabTopOffset + TabHeight + Gap;
            float contentBottomMargin = BottomPad + BottomBarHeight + Gap;
            _contentArea.offsetMin = new Vector2(SidePad, contentBottomMargin);
            _contentArea.offsetMax = new Vector2(-SidePad, -contentTopMargin);

            BuildTabs(tabBarRt);
            BuildBottomBar(windowRt, SidePad, BottomPad, BottomBarHeight);
            ShowTab(0);
        }

        private void BuildHeader(RectTransform window, float sidePad, float topPad, float height)
        {
            var headerGo = new GameObject("Header", typeof(RectTransform));
            var hrt = headerGo.GetComponent<RectTransform>();
            hrt.SetParent(window, false);
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.offsetMin = new Vector2(sidePad, -(topPad + height));
            hrt.offsetMax = new Vector2(-sidePad, -topPad);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            var trt = titleGo.GetComponent<RectTransform>();
            trt.SetParent(hrt, false);
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(0f, 0f);
            trt.offsetMax = new Vector2(-90f, 0f);
            var t = titleGo.AddComponent<Text>();
            t.text = "설정";
            t.fontSize = 22;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = JCJUiColors.HudPrimaryText;
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var closeHolder = new GameObject("CloseHolder", typeof(RectTransform));
            var crt = closeHolder.GetComponent<RectTransform>();
            crt.SetParent(hrt, false);
            crt.anchorMin = new Vector2(1f, 0.5f);
            crt.anchorMax = new Vector2(1f, 0.5f);
            crt.pivot = new Vector2(1f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 0f);
            crt.sizeDelta = new Vector2(78f, 30f);
            var closeBtn = SettingsUiBuilder.CreateButton(crt, "CloseBtn", "닫기", () => SetVisible(false), 14);
            var cbrt = closeBtn.GetComponent<RectTransform>();
            cbrt.anchorMin = Vector2.zero;
            cbrt.anchorMax = Vector2.one;
            cbrt.offsetMin = Vector2.zero;
            cbrt.offsetMax = Vector2.zero;
        }

        private void BuildTabs(RectTransform tabBarRt)
        {
            _tabs.Clear();
            _tabContents.Clear();
            _tabButtons.Clear();

            // 탭을 추가할 때는 ISettingsTab 구현체만 등록하면 버튼과 콘텐츠 생성 흐름을 공유한다.
            switch (_profile)
            {
                case SettingsPanelProfile.BattleMinimal:
                    _tabs.Add(new SettingsTabCamera(dpiOnly: true));
                    _tabs.Add(new SettingsTabSound());
                    _tabs.Add(new SettingsTabKeys());
                    break;
                default:
                    _tabs.Add(new SettingsTabCamera());
                    _tabs.Add(new SettingsTabMinimap());
                    _tabs.Add(new SettingsTabCustomize());
                    _tabs.Add(new SettingsTabSound());
                    _tabs.Add(new SettingsTabKeys());
                    break;
            }

            for (int i = 0; i < _tabs.Count; i++)
            {
                int idx = i;
                var tab = _tabs[i];
                var btn = SettingsUiBuilder.CreateButton(tabBarRt, "TabBtn_" + tab.Title, tab.Title, () => ShowTab(idx), 14);
                _tabButtons.Add(btn);

                var contentGo = tab.Build(_contentArea, _settings);
                contentGo.SetActive(false);
                _tabContents.Add(contentGo);
            }
        }

        private void BuildBottomBar(RectTransform window, float sidePad, float bottomPad, float height)
        {
            var barGo = new GameObject("BottomBar", typeof(RectTransform));
            var rt = barGo.GetComponent<RectTransform>();
            rt.SetParent(window, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(sidePad, bottomPad);
            rt.offsetMax = new Vector2(-sidePad, bottomPad + height);

            var hlg = barGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = 8f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleRight;

            CreateBottomBarButton(rt, "ResetHolder", "기본값으로", 120f, ResetAll);
            CreateBottomBarButton(rt, "SaveHolder", "저장", 100f, SaveAll);
        }

        private void ResetAll()
        {
            _settings?.ResetToDefaults();
            if (_profile != SettingsPanelProfile.BattleMinimal)
                _customize?.ResetToDefaults();
        }

        private void SaveAll()
        {
            _settings?.Save();
            if (_profile != SettingsPanelProfile.BattleMinimal)
                _customize?.Save();
        }

        private static void CreateBottomBarButton(RectTransform parent, string holderName, string label, float width, System.Action onClick)
        {
            var holder = new GameObject(holderName, typeof(RectTransform));
            var holderRt = holder.GetComponent<RectTransform>();
            holderRt.SetParent(parent, false);
            var le = holder.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.flexibleWidth = 0f;

            var btn = SettingsUiBuilder.CreateButton(holderRt, "Btn", label, onClick, 14);
            var brt = btn.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
        }

        private void ShowTab(int index)
        {
            if (index < 0 || index >= _tabContents.Count) return;
            _activeIndex = index;
            for (int i = 0; i < _tabContents.Count; i++)
            {
                if (_tabContents[i] != null) _tabContents[i].SetActive(i == index);
                if (_tabButtons[i] != null)
                {
                    var img = _tabButtons[i].GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = i == index
                            ? new Color(0.42f, 0.55f, 0.85f, 1f)
                            : new Color(0.18f, 0.22f, 0.30f, 1f);
                    }
                }
            }
        }

        private void HandleChanged(SettingsData data)
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i] != null) _tabs[i].Refresh(data);
            }
        }

        private static void EnsureAudioBinder(ISettingsService settings)
        {
            if (settings is not SettingsService service) return;
            if (service.GetComponent<AudioSettingsBinder>() == null)
                service.gameObject.AddComponent<AudioSettingsBinder>();
        }
    }
}
