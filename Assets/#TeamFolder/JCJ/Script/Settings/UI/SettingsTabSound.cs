using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// MASTER / BGM / VFX 볼륨 슬라이더 탭. 발소리를 포함한 모든 효과음은 VFX에 포함된다.
    /// </summary>
    public class SettingsTabSound : ISettingsTab
    {
        public string Title => "사운드";

        private ISettingsService _settings;
        private Slider _master;
        private Slider _bgm;
        private Slider _vfx;
        private Text _masterValue;
        private Text _bgmValue;
        private Text _vfxValue;

        public GameObject Build(RectTransform contentArea, ISettingsService settings)
        {
            _settings = settings;
            var section = SettingsUiBuilder.CreateSection(contentArea, "사운드 설정");
            var rt = (RectTransform)section.transform;

            BuildVolumeRow(rt, "MASTER (전체)", JcjAudioVolume.DefaultMaster,
                () => _settings.Data.masterVolume,
                v => _settings.Mutate(d => d.masterVolume = v),
                out _master, out _masterValue);

            BuildVolumeRow(rt, "BGM", JcjAudioVolume.DefaultBgm,
                () => _settings.Data.bgmVolume,
                v => _settings.Mutate(d => d.bgmVolume = v),
                out _bgm, out _bgmValue);

            BuildVolumeRow(rt, "VFX (효과음·발소리)", JcjAudioVolume.DefaultVfx,
                () => _settings.Data.vfxVolume,
                v => _settings.Mutate(d => d.vfxVolume = v),
                out _vfx, out _vfxValue);

            Refresh(_settings.Data);
            return section;
        }

        private void BuildVolumeRow(
            RectTransform parent,
            string label,
            float defaultValue,
            System.Func<float> read,
            System.Action<float> write,
            out Slider slider,
            out Text valueLabel)
        {
            var content = SettingsUiBuilder.CreateLabeledRow(parent, label);
            var ctRt = (RectTransform)content.transform;

            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            var srt = sliderGo.GetComponent<RectTransform>();
            srt.SetParent(ctRt, false);
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(0.78f, 1f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            slider = SettingsUiBuilder.CreateSlider(srt, "Slider", 0f, 1f, read(), write);
            var sensRt = slider.GetComponent<RectTransform>();
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
            valueLabel = valueGo.AddComponent<Text>();
            valueLabel.fontSize = 14;
            valueLabel.alignment = TextAnchor.MiddleLeft;
            valueLabel.color = JCJUiColors.HudPrimaryText;
            valueLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            UpdateLabel(valueLabel, read());
        }

        private static void UpdateLabel(Text label, float v)
        {
            if (label != null) label.text = Mathf.RoundToInt(v * 100f) + "%";
        }

        public void Refresh(SettingsData data)
        {
            if (data == null) return;
            if (_master != null && !Mathf.Approximately(_master.value, data.masterVolume))
                _master.SetValueWithoutNotify(data.masterVolume);
            UpdateLabel(_masterValue, data.masterVolume);

            if (_bgm != null && !Mathf.Approximately(_bgm.value, data.bgmVolume))
                _bgm.SetValueWithoutNotify(data.bgmVolume);
            UpdateLabel(_bgmValue, data.bgmVolume);

            if (_vfx != null && !Mathf.Approximately(_vfx.value, data.vfxVolume))
                _vfx.SetValueWithoutNotify(data.vfxVolume);
            UpdateLabel(_vfxValue, data.vfxVolume);
        }
    }
}
