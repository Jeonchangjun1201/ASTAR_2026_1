using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Script.Arena
{
    public class ArenaSkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private ArenaPrepHud _owner;
        private ArenaSkillNodeDefinition _definition;
        private Button _button;
        private Text _titleText;
        private Text _costText;
        private Image _gemImage;
        private Image _ringImage;
        private bool _purchased;

        public ArenaNodeId NodeId => _definition.NodeId;

        public void Setup(ArenaPrepHud owner, ArenaSkillNodeDefinition definition)
        {
            _owner = owner;
            _definition = definition;
            _button = GetComponent<Button>();
            EnsureVisuals();
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(HandleClick);
                _button.transition = Selectable.Transition.None;
            }

            Refresh(string.Empty, false, false);
        }

        public void Refresh(string statusText, bool interactable, bool purchased)
        {
            if (_titleText != null)
            {
                _titleText.text = _definition.DisplayName;
            }

            if (_costText != null)
            {
                _costText.text = purchased ? "완료" : _definition.Cost.ToString();
            }

            if (_button != null)
            {
                _button.interactable = interactable;
            }

            if (_gemImage != null)
            {
                _gemImage.color = purchased
                    ? new Color(0.28f, 0.86f, 0.62f, 1f)
                    : interactable
                        ? new Color(0.92f, 0.82f, 0.36f, 1f)
                        : new Color(0.48f, 0.52f, 0.60f, 0.95f);
            }

            if (_ringImage != null)
            {
                _ringImage.color = purchased
                    ? new Color(1.00f, 0.96f, 0.78f, 0.95f)
                    : interactable
                        ? new Color(1.00f, 0.92f, 0.55f, 0.95f)
                        : new Color(0.26f, 0.30f, 0.38f, 0.85f);
            }

            if (_purchased != purchased && purchased)
            {
                transform.DOKill();
                transform.localScale = Vector3.one;
                transform.DOPunchScale(Vector3.one * 0.18f, 0.28f, 5, 0.85f);
            }

            _purchased = purchased;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(1.08f, 0.12f).SetEase(Ease.OutBack);
            _owner?.ShowTooltip(ArenaSkillCatalog.BuildTooltip(_definition));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(1f, 0.12f).SetEase(Ease.OutQuad);
            _owner?.HideTooltip();
        }

        private void HandleClick()
        {
            _owner?.TryPurchaseNode(_definition.NodeId);
        }

        private void EnsureVisuals()
        {
            var rootRect = GetComponent<RectTransform>();
            var background = GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0f, 0f, 0f, 0f);
                background.raycastTarget = true;
            }

            if (transform.Find("Ring") == null)
            {
                var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
                ring.transform.SetParent(transform, false);
                var ringRect = ring.GetComponent<RectTransform>();
                ringRect.anchorMin = new Vector2(0.5f, 0.5f);
                ringRect.anchorMax = new Vector2(0.5f, 0.5f);
                ringRect.pivot = new Vector2(0.5f, 0.5f);
                ringRect.anchoredPosition = new Vector2(0f, 0f);
                ringRect.sizeDelta = new Vector2(62f, 62f);
                var ringImage = ring.GetComponent<Image>();
                ringImage.sprite = _TeamFolder.JCJ.Script.SettingsUiBuilder.GetRoundedSprite();
                ringImage.type = Image.Type.Sliced;
                ringImage.color = new Color(0.26f, 0.30f, 0.38f, 0.85f);
                ringRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            if (transform.Find("Gem") == null)
            {
                var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
                gem.transform.SetParent(transform, false);
                var gemRect = gem.GetComponent<RectTransform>();
                gemRect.anchorMin = new Vector2(0.5f, 0.5f);
                gemRect.anchorMax = new Vector2(0.5f, 0.5f);
                gemRect.pivot = new Vector2(0.5f, 0.5f);
                gemRect.anchoredPosition = new Vector2(0f, 0f);
                gemRect.sizeDelta = new Vector2(44f, 44f);
                var gemImage = gem.GetComponent<Image>();
                gemImage.sprite = _TeamFolder.JCJ.Script.SettingsUiBuilder.GetRoundedSprite();
                gemImage.type = Image.Type.Sliced;
                gemImage.color = new Color(0.48f, 0.52f, 0.60f, 0.95f);
                gemRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            if (transform.Find("Title") == null)
            {
                var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
                title.transform.SetParent(transform, false);
                var titleRect = title.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.5f, 0f);
                titleRect.anchorMax = new Vector2(0.5f, 0f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -4f);
                titleRect.sizeDelta = new Vector2(110f, 20f);
                var titleText = title.GetComponent<Text>();
                titleText.font = _owner.ResolveFont();
                titleText.fontSize = 12;
                titleText.alignment = TextAnchor.UpperCenter;
                titleText.color = _TeamFolder.JCJ.Script.JCJUiColors.HudPrimaryText;
            }

            if (transform.Find("Cost") == null)
            {
                var cost = new GameObject("Cost", typeof(RectTransform), typeof(Text));
                cost.transform.SetParent(transform, false);
                var costRect = cost.GetComponent<RectTransform>();
                costRect.anchorMin = new Vector2(0.5f, 0.5f);
                costRect.anchorMax = new Vector2(0.5f, 0.5f);
                costRect.pivot = new Vector2(0.5f, 0.5f);
                costRect.anchoredPosition = new Vector2(0f, 0f);
                costRect.sizeDelta = new Vector2(52f, 24f);
                var costText = cost.GetComponent<Text>();
                costText.font = _owner.ResolveFont();
                costText.fontSize = 12;
                costText.alignment = TextAnchor.MiddleCenter;
                costText.color = new Color(0.07f, 0.08f, 0.10f, 1f);
                costText.fontStyle = FontStyle.Bold;
            }

            _ringImage = transform.Find("Ring").GetComponent<Image>();
            _gemImage = transform.Find("Gem").GetComponent<Image>();
            _titleText = transform.Find("Title").GetComponent<Text>();
            _costText = transform.Find("Cost").GetComponent<Text>();
            if (rootRect != null)
            {
                rootRect.sizeDelta = new Vector2(98f, 98f);
            }
        }
    }
}
