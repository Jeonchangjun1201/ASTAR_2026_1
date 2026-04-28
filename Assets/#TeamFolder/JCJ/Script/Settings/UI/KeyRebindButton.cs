using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 버튼을 누르면 다음 입력을 감지해 설정 데이터의 키 경로로 저장하는 UI 컴포넌트.
    /// </summary>
    public class KeyRebindButton : MonoBehaviour
    {
        private Button _btn;
        private Text _label;
        private Func<string> _readPath;
        private Action<string> _writePath;
        private string _displayPrefix;
        private InputActionRebindingExtensions.RebindingOperation _operation;

        public static KeyRebindButton Create(RectTransform parent, string label, Func<string> readPath, Action<string> writePath)
        {
            var go = new GameObject($"KeyRebind_{label}", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var krb = go.AddComponent<KeyRebindButton>();
            krb._displayPrefix = label;
            krb._readPath = readPath;
            krb._writePath = writePath;
            krb.BuildUi();
            return krb;
        }

        private void BuildUi()
        {
            _btn = SettingsUiBuilder.CreateButton(GetComponent<RectTransform>(), "RebindBtn", FormatLabel(_readPath != null ? _readPath() : string.Empty), StartRebind, fontSize: 14);
            var btnRt = _btn.GetComponent<RectTransform>();
            btnRt.anchorMin = Vector2.zero;
            btnRt.anchorMax = Vector2.one;
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            _label = _btn.GetComponentInChildren<Text>();
        }

        public void Refresh()
        {
            if (_label == null || _readPath == null) return;
            _label.text = FormatLabel(_readPath());
        }

        private string FormatLabel(string path)
        {
            if (string.IsNullOrEmpty(path)) return "(없음)";
            return ShortenPath(path);
        }

        private static string ShortenPath(string path)
        {
            int slash = path.LastIndexOf('/');
            if (slash >= 0 && slash < path.Length - 1) return path.Substring(slash + 1).ToUpperInvariant();
            return path;
        }

        private void StartRebind()
        {
            if (_writePath == null || _readPath == null) return;
            if (_operation != null) { _operation.Cancel(); _operation.Dispose(); _operation = null; }

            if (_label != null) _label.text = "...키 입력 대기...";

            // 실제 플레이 입력 맵을 건드리지 않기 위해 임시 InputAction으로 새 바인딩만 감지한다.
            var dummy = new InputAction(name: "JCJRebindDummy", InputActionType.Button);
            dummy.AddBinding(_readPath());
            _operation = dummy.PerformInteractiveRebinding(0)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.05f)
                .OnComplete(op =>
                {
                    string newPath = op.selectedControl != null ? op.selectedControl.path : op.action.bindings[0].effectivePath;
                    _writePath?.Invoke(newPath);
                    Refresh();
                    op.Dispose();
                    _operation = null;
                    dummy.Dispose();
                })
                .OnCancel(op =>
                {
                    Refresh();
                    op.Dispose();
                    _operation = null;
                    dummy.Dispose();
                });
            _operation.Start();
        }

        private void OnDestroy()
        {
            if (_operation != null) { _operation.Cancel(); _operation.Dispose(); _operation = null; }
        }
    }
}
