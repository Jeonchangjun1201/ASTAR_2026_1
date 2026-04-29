using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 설정 창에서 내 캐릭터 몸 색상을 프리셋 스와치로 선택하는 커스터마이즈 탭.
    /// </summary>
    public class SettingsTabCustomize : ISettingsTab
    {
        public string Title => "커스텀";

        // 자유 색상 입력 대신 프리셋만 제공해서 플레이 중에도 UI가 단순하고 팀 색상 관리가 쉽다.
        private static readonly Color[] BodyColorPresets =
        {
            new(0.55f, 0.95f, 0.70f, 1f),
            new(1.00f, 0.45f, 0.45f, 1f),
            new(0.45f, 0.75f, 1.00f, 1f),
            new(1.00f, 0.85f, 0.35f, 1f),
            new(0.85f, 0.55f, 1.00f, 1f),
            new(0.94f, 0.88f, 0.74f, 1f),
            new(1.00f, 1.00f, 1.00f, 1f),
        };

        private ICustomizeService _customize;
        private readonly Button[] _colorButtons = new Button[BodyColorPresets.Length];
        private readonly Image[] _colorMarkers = new Image[BodyColorPresets.Length];

        public GameObject Build(RectTransform contentArea, ISettingsService settings)
        {
            _customize = CustomizeService.EnsureInstance();

            var section = SettingsUiBuilder.CreateSection(contentArea, "캐릭터 색상");
            // 탭 GameObject가 사라질 때 이벤트 구독도 같이 해제해 설정창 재생성 시 중복 호출을 막는다.
            var lifecycle = section.AddComponent<TabLifecycle>();
            lifecycle.OnDestroyed += Unsubscribe;

            if (_customize != null) _customize.OnChanged += HandleCustomizeChanged;

            var rt = (RectTransform)section.transform;
            BuildColor(rt);
            RefreshCustomize(_customize?.Data);
            return section;
        }

        private void BuildColor(RectTransform parent)
        {
            // 미니맵 색상 탭과 같은 스와치 UI를 사용해 설정창 전체 조작감을 맞춘다.
            var content = SettingsUiBuilder.CreateLabeledRow(parent, "몸 색상");
            var ctRt = (RectTransform)content.transform;

            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            for (int i = 0; i < BodyColorPresets.Length; i++)
            {
                int idx = i;
                var swatchHolder = new GameObject("BodyColorBtnHolder_" + i, typeof(RectTransform));
                var br = swatchHolder.GetComponent<RectTransform>();
                br.SetParent(ctRt, false);
                var le = swatchHolder.AddComponent<LayoutElement>();
                le.preferredWidth = 36f;
                le.preferredHeight = 28f;
                _colorButtons[i] = SettingsUiBuilder.CreateButton(br, "Swatch", "", () =>
                {
                    var c = BodyColorPresets[idx];
                    // 저장 데이터만 바꾸면 CustomizeService가 OnChanged를 발생시켜 로컬 캐릭터 비주얼이 즉시 갱신된다.
                    _customize?.Mutate(d => d.bodyColor = c);
                });
                var bRt = _colorButtons[i].GetComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero;
                bRt.anchorMax = Vector2.one;
                bRt.offsetMin = Vector2.zero;
                bRt.offsetMax = Vector2.zero;
                _colorMarkers[i] = _colorButtons[i].GetComponent<Image>();
                _colorMarkers[i].color = BodyColorPresets[i];
            }
        }

        public void Refresh(SettingsData data)
        {
            RefreshCustomize(_customize?.Data);
        }

        private void HandleCustomizeChanged(CustomizeData data)
        {
            RefreshCustomize(data);
        }

        private void RefreshCustomize(CustomizeData data)
        {
            if (data == null) return;
            for (int i = 0; i < BodyColorPresets.Length; i++)
            {
                if (_colorMarkers[i] == null) continue;
                // 현재 저장된 색상과 같은 스와치는 selectedColor를 밝게 해 선택 상태를 보여준다.
                bool active = ApproximatelyEqual(data.bodyColor, BodyColorPresets[i]);
                _colorMarkers[i].color = BodyColorPresets[i];
                var btn = _colorButtons[i];
                if (btn != null)
                {
                    var col = btn.colors;
                    col.normalColor = BodyColorPresets[i];
                    col.highlightedColor = Color.Lerp(BodyColorPresets[i], Color.white, 0.2f);
                    col.pressedColor = Color.Lerp(BodyColorPresets[i], Color.black, 0.15f);
                    col.selectedColor = active ? Color.white : col.normalColor;
                    btn.colors = col;
                }
            }
        }

        private void Unsubscribe()
        {
            if (_customize != null) _customize.OnChanged -= HandleCustomizeChanged;
        }

        private static bool ApproximatelyEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;
        }
    }
}
