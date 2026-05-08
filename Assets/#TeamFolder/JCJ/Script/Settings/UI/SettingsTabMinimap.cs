using UnityEngine;
using UnityEngine.UI;

// 미니맵 옵션을 표시하고 수정하는 탭 UI.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미니맵 크기, 화면 위치, 플레이어 표시 색상을 변경하는 설정 탭.
    /// </summary>
    public class SettingsTabMinimap : ISettingsTab
    {
        public string Title => "미니맵";

        private static readonly Color[] PlayerColorPresets =
        {
            new(0.55f, 0.95f, 0.70f, 1f),
            new(1.00f, 0.45f, 0.45f, 1f),
            new(0.45f, 0.75f, 1.00f, 1f),
            new(1.00f, 0.85f, 0.35f, 1f),
            new(0.85f, 0.55f, 1.00f, 1f),
            new(1.00f, 1.00f, 1.00f, 1f),
        };

        private ISettingsService _settings;
        private Slider _sizeSlider;
        private Text _sizeValue;
        private readonly Button[] _anchorButtons = new Button[4];
        private readonly Image[] _anchorMarkers = new Image[4];
        private readonly Button[] _colorButtons = new Button[PlayerColorPresets.Length];
        private readonly Image[] _colorMarkers = new Image[PlayerColorPresets.Length];

        public GameObject Build(RectTransform contentArea, ISettingsService settings)
        {
            _settings = settings;
            var section = SettingsUiBuilder.CreateSection(contentArea, "미니맵 설정");
            var rt = (RectTransform)section.transform;

            BuildSize(rt);
            BuildAnchor(rt);
            BuildColor(rt);

            Refresh(_settings.Data);
            return section;
        }

        private void BuildSize(RectTransform parent)
        {
            var content = SettingsUiBuilder.CreateLabeledRow(parent, "미니맵 크기");
            var ctRt = (RectTransform)content.transform;

            var sliderHolder = new GameObject("Slider", typeof(RectTransform));
            var srt = sliderHolder.GetComponent<RectTransform>();
            srt.SetParent(ctRt, false);
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = new Vector2(0.78f, 1f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            _sizeSlider = SettingsUiBuilder.CreateSlider(srt, "Size", 120f, 360f,
                _settings.Data.minimapSize,
                v => { _settings.Mutate(d => d.minimapSize = v); UpdateSizeLabel(v); });
            var rrt = _sizeSlider.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;

            var valueGo = new GameObject("Value", typeof(RectTransform));
            var vrt = valueGo.GetComponent<RectTransform>();
            vrt.SetParent(ctRt, false);
            vrt.anchorMin = new Vector2(0.78f, 0f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.offsetMin = new Vector2(8f, 0f);
            vrt.offsetMax = Vector2.zero;
            _sizeValue = valueGo.AddComponent<Text>();
            _sizeValue.fontSize = 14;
            _sizeValue.alignment = TextAnchor.MiddleLeft;
            _sizeValue.color = JCJUiColors.HudPrimaryText;
            _sizeValue.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            UpdateSizeLabel(_settings.Data.minimapSize);
        }

        private void UpdateSizeLabel(float v)
        {
            if (_sizeValue != null) _sizeValue.text = $"{v:0}";
        }

        private void BuildAnchor(RectTransform parent)
        {
            // 네 모서리 프리셋을 버튼으로 제공하고, 선택 상태는 Refresh에서 색으로 표시한다.
            var content = SettingsUiBuilder.CreateLabeledRow(parent, "미니맵 위치");
            var ctRt = (RectTransform)content.transform;

            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            string[] labels = { "좌상", "우상", "좌하", "우하" };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var btnHolder = new GameObject("AnchorBtnHolder_" + labels[i], typeof(RectTransform));
                var br = btnHolder.GetComponent<RectTransform>();
                br.SetParent(ctRt, false);
                var le = btnHolder.AddComponent<LayoutElement>();
                le.preferredWidth = 64f;
                le.preferredHeight = 28f;
                _anchorButtons[i] = SettingsUiBuilder.CreateButton(br, "Btn", labels[i],
                    () => _settings.Mutate(d => d.minimapAnchor = (MinimapAnchorPreset)idx), 13);
                var bRt = _anchorButtons[i].GetComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero;
                bRt.anchorMax = Vector2.one;
                bRt.offsetMin = Vector2.zero;
                bRt.offsetMax = Vector2.zero;
                _anchorMarkers[i] = _anchorButtons[i].GetComponent<Image>();
            }
        }

        private void BuildColor(RectTransform parent)
        {
            // 색상은 자유 입력 대신 프리셋 스와치로 제한해 HUD 가독성을 유지한다.
            var content = SettingsUiBuilder.CreateLabeledRow(parent, "내 색상");
            var ctRt = (RectTransform)content.transform;

            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            for (int i = 0; i < PlayerColorPresets.Length; i++)
            {
                int idx = i;
                var swatchHolder = new GameObject("ColorBtnHolder_" + i, typeof(RectTransform));
                var br = swatchHolder.GetComponent<RectTransform>();
                br.SetParent(ctRt, false);
                var le = swatchHolder.AddComponent<LayoutElement>();
                le.preferredWidth = 36f;
                le.preferredHeight = 28f;
                _colorButtons[i] = SettingsUiBuilder.CreateButton(br, "Swatch", "", () =>
                {
                    var c = PlayerColorPresets[idx];
                    _settings.Mutate(d => d.minimapPlayerColor = c);
                });
                var bRt = _colorButtons[i].GetComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero;
                bRt.anchorMax = Vector2.one;
                bRt.offsetMin = Vector2.zero;
                bRt.offsetMax = Vector2.zero;
                _colorMarkers[i] = _colorButtons[i].GetComponent<Image>();
                _colorMarkers[i].color = PlayerColorPresets[i];
            }
        }

        public void Refresh(SettingsData data)
        {
            if (data == null) return;
            if (_sizeSlider != null && !Mathf.Approximately(_sizeSlider.value, data.minimapSize))
                _sizeSlider.SetValueWithoutNotify(data.minimapSize);
            UpdateSizeLabel(data.minimapSize);

            for (int i = 0; i < 4; i++)
            {
                if (_anchorMarkers[i] == null) continue;
                bool active = (int)data.minimapAnchor == i;
                var c = active ? new Color(0.42f, 0.55f, 0.85f, 1f) : new Color(0.18f, 0.22f, 0.30f, 1f);
                _anchorMarkers[i].color = c;
            }

            for (int i = 0; i < PlayerColorPresets.Length; i++)
            {
                if (_colorMarkers[i] == null) continue;
                bool active = ApproximatelyEqual(data.minimapPlayerColor, PlayerColorPresets[i]);
                _colorMarkers[i].color = PlayerColorPresets[i];
                var btn = _colorButtons[i];
                if (btn != null)
                {
                    var col = btn.colors;
                    col.normalColor = PlayerColorPresets[i];
                    col.highlightedColor = Color.Lerp(PlayerColorPresets[i], Color.white, 0.2f);
                    col.pressedColor = Color.Lerp(PlayerColorPresets[i], Color.black, 0.15f);
                    col.selectedColor = active ? Color.white : col.normalColor;
                    btn.colors = col;
                }
            }
        }

        private static bool ApproximatelyEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;
        }
    }
}
