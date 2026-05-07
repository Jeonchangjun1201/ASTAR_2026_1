using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Battle
{
    public class BattleDamagePopup : MonoBehaviour
        , IBattlePoolAware
    {
        private Text _text;
        private Canvas _canvas;
        private float _elapsed;
        private const float Duration = 0.8f;
        private const float RiseSpeed = 1.5f;
        private Vector3 _startPos;

        public void Initialize(int damage, bool headshot)
        {
            EnsureBuilt();
            _startPos = transform.position;
            _elapsed = 0f;

            _text.text = damage.ToString();
            _text.fontSize = headshot ? BattlePrototypeManager.PopupHeadshotFontSize : BattlePrototypeManager.PopupFontSize;
            _text.fontStyle = headshot ? FontStyle.Bold : FontStyle.Normal;
            _text.color = headshot ? new Color(1f, 0.15f, 0.15f) : new Color(1f, 0.9f, 0.3f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / Duration;

            if (t >= 1f)
            {
                BattlePoolManager.Release(gameObject);
                return;
            }

            transform.position = _startPos + Vector3.up * (RiseSpeed * t);

            if (Camera.main != null)
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

            if (_text != null)
            {
                var c = _text.color;
                c.a = 1f - t * t;
                _text.color = c;
            }
        }

        public void OnSpawnedFromPool()
        {
            _elapsed = 0f;
        }

        public void OnReturnedToPool()
        {
            _elapsed = 0f;
        }

        private void EnsureBuilt()
        {
            if (_text != null) return;

            var canvasObj = new GameObject("PopupCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 600;

            var rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1.5f, 0.4f);
            rt.localScale = Vector3.one * BattlePrototypeManager.PopupScale;

            var textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(canvasObj.transform, false);

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            _text = textObj.GetComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_text.font == null) _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;
        }
    }
}
