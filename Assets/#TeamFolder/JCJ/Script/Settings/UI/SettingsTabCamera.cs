using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 마우스 감도, Y축 반전, 피치 잠금 같은 카메라 옵션을 표시하고 변경한다.
    /// </summary>
    public class SettingsTabCamera : ISettingsTab
    {
        public string Title => "카메라";

        private ISettingsService _settings;
        private Slider _sensitivity;
        private Toggle _invertY;
        private Toggle _lockPitch;
        private Text _sensitivityValue;

        public GameObject Build(RectTransform contentArea, ISettingsService settings)
        {
            _settings = settings;

            var section = SettingsUiBuilder.CreateSection(contentArea, "카메라 설정");
            var rt = (RectTransform)section.transform;

            BuildSensitivityRow(rt);
            BuildToggleRow(rt, "Y축 반전", v => _settings.Mutate(d => d.invertY = v), out _invertY);
            BuildToggleRow(rt, "위/아래 시점 고정", v => _settings.Mutate(d => d.lockPitch = v), out _lockPitch);

            Refresh(_settings.Data);
            return section;
        }

        private void BuildSensitivityRow(RectTransform parent)
        {
            // 슬라이더와 숫자 라벨을 한 줄에 배치해 변경 값을 즉시 확인하게 한다.
            var content = SettingsUiBuilder.CreateLabeledRow(parent, "마우스 감도");
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
            // 외부에서 설정이 바뀐 경우 UI 이벤트를 다시 발생시키지 않고 표시값만 동기화한다.
            if (_sensitivity != null && !Mathf.Approximately(_sensitivity.value, data.cameraSensitivity))
                _sensitivity.SetValueWithoutNotify(data.cameraSensitivity);
            UpdateSensitivityLabel(data.cameraSensitivity);
            if (_invertY != null) _invertY.SetIsOnWithoutNotify(data.invertY);
            if (_lockPitch != null) _lockPitch.SetIsOnWithoutNotify(data.lockPitch);
        }
    }
}
