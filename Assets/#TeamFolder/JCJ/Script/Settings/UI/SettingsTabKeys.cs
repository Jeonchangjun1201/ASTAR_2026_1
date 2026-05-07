using System.Collections.Generic;
using UnityEngine;

// 키 바인딩을 표시하고 수정하는 탭 UI.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 설정 패널 안에서 이동, 점프, 스프린트, 메뉴 키를 다시 지정하는 탭을 만든다.
    /// </summary>
    public class SettingsTabKeys : ISettingsTab
    {
        public string Title => "키 설정";

        private ISettingsService _settings;
        private readonly List<KeyRebindButton> _buttons = new();

        public GameObject Build(RectTransform contentArea, ISettingsService settings)
        {
            _settings = settings;

            // 각 행은 현재 키 경로를 읽는 함수와 새 키 경로를 저장하는 함수를 함께 넘긴다.
            var section = SettingsUiBuilder.CreateSection(contentArea, "키 바인딩");
            var rt = (RectTransform)section.transform;

            AddRow(rt, "앞으로 이동", () => _settings.Data.keyMoveUp,    p => _settings.Mutate(d => d.keyMoveUp = p));
            AddRow(rt, "뒤로 이동",   () => _settings.Data.keyMoveDown,  p => _settings.Mutate(d => d.keyMoveDown = p));
            AddRow(rt, "왼쪽 이동",   () => _settings.Data.keyMoveLeft,  p => _settings.Mutate(d => d.keyMoveLeft = p));
            AddRow(rt, "오른쪽 이동", () => _settings.Data.keyMoveRight, p => _settings.Mutate(d => d.keyMoveRight = p));
            AddRow(rt, "점프",        () => _settings.Data.keyJump,      p => _settings.Mutate(d => d.keyJump = p));
            AddRow(rt, "스프린트",    () => _settings.Data.keySprint,    p => _settings.Mutate(d => d.keySprint = p));
            AddRow(rt, "메뉴",        () => _settings.Data.keyMenu,      p => _settings.Mutate(d => d.keyMenu = p));

            return section;
        }

        private void AddRow(RectTransform parent, string label, System.Func<string> read, System.Action<string> write)
        {
            var content = SettingsUiBuilder.CreateLabeledRow(parent, label);
            var ctRt = (RectTransform)content.transform;
            var btn = KeyRebindButton.Create(ctRt, label, read, write);
            _buttons.Add(btn);
        }

        public void Refresh(SettingsData data)
        {
            foreach (var b in _buttons)
            {
                if (b != null) b.Refresh();
            }
        }
    }
}
