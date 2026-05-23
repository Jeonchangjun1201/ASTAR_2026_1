using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Script
{
    public class CountdownUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private float _stepSeconds = 1f;
        [SerializeField] private int _startNumber = 3;
        [SerializeField] private string _goText = "GO!";
        [SerializeField] private float _goHoldSeconds = 0.6f;
        [SerializeField] private int _canvasSortOrder = 250;

        public event Action<int> OnTick;
        public event Action OnGo;
        public event Action OnComplete;

        public float StepSeconds => Mathf.Max(0.05f, _stepSeconds);
        public int StartNumber => Mathf.Max(0, _startNumber);
        public string GoText => string.IsNullOrWhiteSpace(_goText) ? "GO!" : _goText;

        private Coroutine _routine;
        private Canvas _canvas;

        private void Awake()
        {
            EnsureVisual();
            HideImmediate();
        }

        public void Begin(int seconds)
        {
            Cancel();
            _routine = StartCoroutine(PlayRoutine(seconds));
        }

        public void Cancel()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            HideImmediate();
        }

        public IEnumerator PlayRoutine(int? secondsOverride = null)
        {
            int seconds = secondsOverride ?? StartNumber;
            seconds = Mathf.Max(0, seconds);
            EnsureVisual();

            for (int i = seconds; i > 0; i--)
            {
                OnTick?.Invoke(i);
                ShowNumber(i);
                yield return new WaitForSeconds(StepSeconds);
            }

            OnGo?.Invoke();
            ShowGo();
            yield return new WaitForSeconds(Mathf.Max(0f, _goHoldSeconds));
            HideImmediate();
            OnComplete?.Invoke();
            _routine = null;
        }

        private void ShowNumber(int value)
        {
            if (_label == null) return;
            _label.gameObject.SetActive(true);
            _label.text = value.ToString();
            _label.color = JCJUiColors.HudAccentBright;
            HudTweenHelpers.BounceCountdown(_label.transform);
        }

        private void ShowGo()
        {
            if (_label == null) return;
            _label.gameObject.SetActive(true);
            _label.text = GoText;
            HudTweenHelpers.GoBurst(_label.transform, _label);
        }

        private void HideImmediate()
        {
            if (_label != null)
            {
                _label.text = string.Empty;
                _label.transform.localScale = Vector3.zero;
                _label.gameObject.SetActive(false);
            }
        }

        private void EnsureVisual()
        {
            if (_label != null) return;

            var canvasGo = new GameObject("CountdownCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = _canvasSortOrder;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("CountdownLabel");
            textGo.transform.SetParent(canvasGo.transform, false);
            var rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900f, 360f);

            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 220f;
            _label.fontStyle = FontStyles.Bold;
            _label.color = JCJUiColors.HudAccentBright;
            _label.outlineColor = Color.black;
            _label.outlineWidth = 0.25f;
            _label.raycastTarget = false;
            _label.font = Resources.Load<TMP_FontAsset>("Fonts/Paperlogy-3Light SDF");
        }
    }
}
