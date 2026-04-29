using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Script
{
    public class SettingsTabCamera : ISettingsTab
    {
        public string Title => "카메라";

        private ISettingsService _settings;
        private Slider _sensitivity;
        private Toggle _verticalRotation;
        private Text _sensitivityValue;

        public GameObject Build(RectTransform contentArea, ISettingsService settings)
        {
            _settings = settings;

            var section = SettingsUiBuilder.CreateSection(contentArea, "카메라 설정");
            var rt = (RectTransform)section.transform;

            BuildSensitivityRow(rt);
            BuildToggleRow(rt, "카메라 세로 회전", v => _settings.Mutate(d => d.lockPitch = !v), out _verticalRotation);

            Refresh(_settings.Data);
            return section;
        }

        private void BuildSensitivityRow(RectTransform parent)
        {
            var content = SettingsUiBuilder.CreateLabeledRow(parent, "DPI");
            var ctRt = (RectTransform)content.transform;

            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            var srt = sliderGo.GetComponent<RectTransform>();
            srt.SetParent(ctRt, false);
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(0.78f, 1f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            _sensitivity = SettingsUiBuilder.CreateSlider(srt, "Sens", 0.05f, 1f,
                _settings.Data.cameraSensitivity,
                v => { _settings.Mutate(d => d.cameraSensitivity = v); UpdateSensitivityLabel(v); });
            var sensRt = _sensitivity.GetComponent<RectTransform>();
            sensRt.anchorMin = Vector2.zero;
            sensRt.anchorMax = Vector2.one;
            sensRt.offsetMin = Vector2.zero;
            sensRt.offsetMax = Vector2.zero;

            var valueGo = new GameObject("Value", typeof(RectTransform));
            var vrt = valueGo.GetComponent<RectTransform>();
            vrt.SetParent(ctRt, false);
            vrt.anchorMin = new Vector2(0.78f, 0f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.offsetMin = new Vector2(8f, 0f);
            vrt.offsetMax = Vector2.zero;
            _sensitivityValue = valueGo.AddComponent<Text>();
            _sensitivityValue.fontSize = 14;
            _sensitivityValue.alignment = TextAnchor.MiddleLeft;
            _sensitivityValue.color = JCJUiColors.HudPrimaryText;
            _sensitivityValue.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            UpdateSensitivityLabel(_settings.Data.cameraSensitivity);
        }

        private void BuildToggleRow(RectTransform parent, string label, System.Action<bool> apply, out Toggle toggleOut)
        {
            var content = SettingsUiBuilder.CreateLabeledRow(parent, label);
            var ctRt = (RectTransform)content.transform;
            var holder = new GameObject("ToggleHolder", typeof(RectTransform));
            var hrt = holder.GetComponent<RectTransform>();
            hrt.SetParent(ctRt, false);
            hrt.anchorMin = Vector2.zero;
            hrt.anchorMax = new Vector2(0f, 1f);
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;
            hrt.sizeDelta = new Vector2(28f, 0f);
            toggleOut = SettingsUiBuilder.CreateToggle(hrt, "Toggle", false, apply);
            var trt = toggleOut.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        private void UpdateSensitivityLabel(float v)
        {
            if (_sensitivityValue != null) _sensitivityValue.text = v.ToString("0.00");
        }

        public void Refresh(SettingsData data)
        {
            if (data == null) return;
            if (_sensitivity != null && !Mathf.Approximately(_sensitivity.value, data.cameraSensitivity))
                _sensitivity.SetValueWithoutNotify(data.cameraSensitivity);
            UpdateSensitivityLabel(data.cameraSensitivity);
            if (_verticalRotation != null) _verticalRotation.SetIsOnWithoutNotify(!data.lockPitch);
        }
    }
}
