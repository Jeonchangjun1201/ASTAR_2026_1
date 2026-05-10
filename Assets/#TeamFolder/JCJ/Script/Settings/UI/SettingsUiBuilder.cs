using System;
using UnityEngine;
using UnityEngine.UI;

// 설정 패널 UI를 코드로 생성하는 빌더.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 설정 패널에서 반복해서 쓰는 버튼, 슬라이더, 토글, 섹션 UI를 코드로 생성하는 유틸리티.
    /// </summary>
    public static class SettingsUiBuilder
    {
        private static readonly Color PanelBg     = new(0.05f, 0.06f, 0.09f, 0.96f);
        private static readonly Color PanelLight  = new(0.10f, 0.12f, 0.16f, 0.95f);
        private static readonly Color AccentLine  = JCJUiColors.HudAccentLine;
        private static readonly Color HeaderText  = JCJUiColors.HudPrimaryText;
        private static readonly Color BodyText    = JCJUiColors.HudPrimaryText;
        private static readonly Color MutedText   = JCJUiColors.HudMutedText;
        private static readonly Color ButtonNormal = new(0.18f, 0.22f, 0.30f, 1f);
        private static readonly Color ButtonHover  = new(0.26f, 0.32f, 0.42f, 1f);
        private static readonly Color ButtonActive = new(0.42f, 0.55f, 0.85f, 1f);

        private static Sprite _roundedSprite;
        private const int RoundedSize = 64;
        private const int RoundedCorner = 14;

        public static Sprite GetRoundedSprite()
        {
            if (_roundedSprite != null) return _roundedSprite;
            // UI 배경과 버튼에 사용할 라운드 사각형 스프라이트를 런타임에 한 번만 만든다.
            var tex = new Texture2D(RoundedSize, RoundedSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            int r = RoundedCorner;
            float r2 = r * r;
            var px = new Color[RoundedSize * RoundedSize];
            var transparent = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < RoundedSize; y++)
            {
                for (int x = 0; x < RoundedSize; x++)
                {
                    int dx = 0, dy = 0;
                    if (x < r) dx = r - x; else if (x >= RoundedSize - r) dx = x - (RoundedSize - r - 1);
                    if (y < r) dy = r - y; else if (y >= RoundedSize - r) dy = y - (RoundedSize - r - 1);
                    float dist2 = dx * dx + dy * dy;
                    float alpha;
                    if (dist2 <= (r - 1) * (r - 1)) alpha = 1f;
                    else if (dist2 >= (r + 0.5f) * (r + 0.5f)) alpha = 0f;
                    else alpha = Mathf.Clamp01((r + 0.5f - Mathf.Sqrt(dist2)));
                    px[y * RoundedSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, true);

            _roundedSprite = Sprite.Create(
                tex,
                new Rect(0, 0, RoundedSize, RoundedSize),
                new Vector2(0.5f, 0.5f),
                100f, 0,
                SpriteMeshType.FullRect,
                new Vector4(r, r, r, r));
            _roundedSprite.hideFlags = HideFlags.HideAndDontSave;
            return _roundedSprite;
        }

        private static Image AddRoundedImage(GameObject host, Color color)
        {
            var img = host.AddComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        public static GameObject CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color? bg = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            AddRoundedImage(go, bg ?? PanelBg);
            return go;
        }

        public static Text CreateText(RectTransform parent, string name, string text, int fontSize, TextAnchor anchor, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.color = color ?? BodyText;
            t.raycastTarget = false;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return t;
        }

        public static Button CreateButton(RectTransform parent, string name, string label, Action onClick, int fontSize = 16)
        {
            // 버튼 본체와 텍스트 라벨을 한 GameObject 트리로 묶어 호출하는 쪽의 배치 코드만 단순하게 둔다.
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var img = AddRoundedImage(go, ButtonNormal);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHover;
            colors.pressedColor = ButtonActive;
            colors.selectedColor = ButtonHover;
            colors.disabledColor = new Color(0.10f, 0.12f, 0.15f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(rt, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(8f, 4f);
            labelRt.offsetMax = new Vector2(-8f, -4f);
            var t = labelGo.AddComponent<Text>();
            t.text = label;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = HeaderText;
            t.raycastTarget = false;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static Slider CreateSlider(RectTransform parent, string name, float min, float max, float current, Action<float> onChanged)
        {
            // Unity 기본 Slider에 필요한 Background, Fill, Handle 구조를 코드에서 모두 구성한다.
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);

            const float HandleWidth = 8f;
            const float HandleHeight = 6f;
            const float TrackMargin = HandleWidth * 0.5f;

            var bgGo = new GameObject("Background", typeof(RectTransform));
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.SetParent(rt, false);
            bgRt.anchorMin = new Vector2(0f, 0.4f);
            bgRt.anchorMax = new Vector2(1f, 0.6f);
            bgRt.offsetMin = new Vector2(TrackMargin, 0f);
            bgRt.offsetMax = new Vector2(-TrackMargin, 0f);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.22f, 0.26f, 0.34f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.SetParent(rt, false);
            fillAreaRt.anchorMin = new Vector2(0f, 0.4f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.6f);
            fillAreaRt.offsetMin = new Vector2(TrackMargin, 0f);
            fillAreaRt.offsetMax = new Vector2(-TrackMargin, 0f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.SetParent(fillAreaRt, false);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = AccentLine;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.SetParent(rt, false);
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(TrackMargin, 0f);
            handleAreaRt.offsetMax = new Vector2(-TrackMargin, 0f);

            var handleGo = new GameObject("Handle", typeof(RectTransform));
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.SetParent(handleAreaRt, false);
            handleRt.sizeDelta = new Vector2(HandleWidth, HandleHeight);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(0.95f, 0.97f, 1.00f, 1f);

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.value = Mathf.Clamp(current, min, max);
            if (onChanged != null) slider.onValueChanged.AddListener(v => onChanged(v));
            return slider;
        }

        public static Toggle CreateToggle(RectTransform parent, string name, bool initial, Action<bool> onChanged)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(28f, 28f);

            var bg = new GameObject("Background", typeof(RectTransform));
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.SetParent(rt, false);
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = AddRoundedImage(bg, ButtonNormal);

            var check = new GameObject("Checkmark", typeof(RectTransform));
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.SetParent(bgRt, false);
            checkRt.anchorMin = new Vector2(0.18f, 0.18f);
            checkRt.anchorMax = new Vector2(0.82f, 0.82f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;
            var checkImg = AddRoundedImage(check, AccentLine);

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = initial;
            if (onChanged != null) toggle.onValueChanged.AddListener(v => onChanged(v));
            return toggle;
        }

        public static GameObject CreateRowSection(RectTransform parent, string name, float height = 32f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            return go;
        }

        public static GameObject CreateLabeledRow(RectTransform parent, string label, float height = 36f)
        {
            var row = CreateRowSection(parent, "Row_" + label, height);
            var rowRt = (RectTransform)row.transform;

            var lbl = new GameObject("Label", typeof(RectTransform));
            var lblRt = lbl.GetComponent<RectTransform>();
            lblRt.SetParent(rowRt, false);
            lblRt.anchorMin = new Vector2(0f, 0f);
            lblRt.anchorMax = new Vector2(0.35f, 1f);
            lblRt.offsetMin = new Vector2(8f, 0f);
            lblRt.offsetMax = new Vector2(-4f, 0f);
            var lblTxt = lbl.AddComponent<Text>();
            lblTxt.text = label;
            lblTxt.fontSize = 14;
            lblTxt.alignment = TextAnchor.MiddleLeft;
            lblTxt.color = MutedText;
            lblTxt.raycastTarget = false;
            lblTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            lblTxt.verticalOverflow = VerticalWrapMode.Overflow;
            lblTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var content = new GameObject("Content", typeof(RectTransform));
            var ctRt = content.GetComponent<RectTransform>();
            ctRt.SetParent(rowRt, false);
            ctRt.anchorMin = new Vector2(0.35f, 0f);
            ctRt.anchorMax = new Vector2(1f, 1f);
            ctRt.offsetMin = new Vector2(4f, 0f);
            ctRt.offsetMax = new Vector2(-8f, 0f);

            return content;
        }

        public static GameObject CreateSection(RectTransform parent, string title)
        {
            var section = new GameObject("Section_" + title, typeof(RectTransform));
            var rt = section.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            var vlg = section.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(12, 12, 10, 10);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            var trt = titleGo.GetComponent<RectTransform>();
            trt.SetParent(rt, false);
            var le = titleGo.AddComponent<LayoutElement>();
            le.preferredHeight = 28f;
            le.flexibleWidth = 1f;
            var t = titleGo.AddComponent<Text>();
            t.text = title;
            t.fontSize = 18;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = HeaderText;
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return section;
        }

        public static Text CreateLabel(RectTransform parent, string name, string text, int fontSize = 14, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = fontSize + 10;
            le.flexibleWidth = 1f;
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = color ?? BodyText;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return t;
        }

        public static Image CreateColorSwatch(RectTransform parent, string name, Color color, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var img = AddRoundedImage(go, color);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return img;
        }

        public static GameObject CreateVerticalGroup(RectTransform parent, string name, float spacing = 6f, RectOffset padding = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.padding = padding ?? new RectOffset(8, 8, 8, 8);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            return go;
        }

        public static GameObject CreateHorizontalGroup(RectTransform parent, string name, float spacing = 6f, RectOffset padding = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.padding = padding ?? new RectOffset(0, 0, 0, 0);
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            return go;
        }

        public static Canvas EnsureCanvas()
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("Canvas (auto)");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;
                go.AddComponent<GraphicRaycaster>();
            }
            ConfigureCanvasScaler(canvas);
            return canvas;
        }

        private static void ConfigureCanvasScaler(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var go = new GameObject("EventSystem (auto)");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
