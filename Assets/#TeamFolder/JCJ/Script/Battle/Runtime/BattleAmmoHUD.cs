using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Battle
{
    public class BattleAmmoHUD : MonoBehaviour
    {
        private BattleWeaponManager _weaponManager;
        private BattleHealth _localHealth;
        private Text _magText;
        private Text _reserveText;
        private Text _weaponNameText;
        private Text _reloadText;
        private Image _reloadBar;
        private RectTransform _reloadBarFill;
        private Text _protectionText;
        private float _reloadDuration;
        private float _reloadStartTime;
        private bool _reloading;
        private Text _hpText;
        private Image _hpFill;
        private float _lastHealth = -1f;

        public void Bind(BattleWeaponManager weaponManager)
        {
            if (_weaponManager != null)
            {
                _weaponManager.OnAmmoChanged -= HandleAmmoChanged;
                _weaponManager.OnReloadStarted -= HandleReloadStarted;
            }

            _weaponManager = weaponManager;

            if (_weaponManager != null)
            {
                _weaponManager.OnAmmoChanged += HandleAmmoChanged;
                _weaponManager.OnReloadStarted += HandleReloadStarted;
                HandleAmmoChanged(_weaponManager.CurrentMagazine, _weaponManager.ReserveAmmo,
                    _weaponManager.CurrentWeapon != null ? _weaponManager.CurrentWeapon.MagazineSize : 0);
                RefreshWeaponName();
                _localHealth = _weaponManager.GetComponent<BattleHealth>();
            }
        }

        private void OnDestroy()
        {
            if (_weaponManager != null)
            {
                _weaponManager.OnAmmoChanged -= HandleAmmoChanged;
                _weaponManager.OnReloadStarted -= HandleReloadStarted;
            }
        }

        private void Awake()
        {
            BuildUI();
        }

        private void Update()
        {
            if (_reloading)
            {
                float elapsed = Time.time - _reloadStartTime;
                float t = Mathf.Clamp01(elapsed / _reloadDuration);
                if (_reloadBarFill != null) _reloadBarFill.anchorMax = new Vector2(t, 1f);
                if (t >= 1f)
                {
                    _reloading = false;
                    if (_reloadText != null) _reloadText.transform.parent.gameObject.SetActive(false);
                }
            }

            UpdateLocalHealth();
            UpdateProtectionIndicator();
        }

        private void UpdateLocalHealth()
        {
            if (_localHealth == null || _hpText == null) return;
            float hp = _localHealth.CurrentHealth;
            if (Mathf.Approximately(hp, _lastHealth)) return;
            _lastHealth = hp;

            float max = _localHealth.MaxHealth;
            float ratio = Mathf.Clamp01(hp / max);
            _hpText.text = Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(max);

            if (ratio > 0.5f) { _hpText.color = Color.white; _hpFill.color = new Color(0.2f, 0.85f, 0.2f); }
            else if (ratio > 0.25f) { _hpText.color = new Color(1f, 0.9f, 0.3f); _hpFill.color = new Color(0.9f, 0.7f, 0.1f); }
            else { _hpText.color = new Color(1f, 0.3f, 0.3f); _hpFill.color = new Color(0.9f, 0.15f, 0.15f); }

            _hpFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        }

        private void HandleAmmoChanged(int magazine, int reserve, int magSize)
        {
            if (_magText != null)
            {
                _magText.text = magazine.ToString();
                float ratio = magSize > 0 ? (float)magazine / magSize : 1f;
                if (ratio <= 0f) _magText.color = new Color(1f, 0.2f, 0.2f);
                else if (ratio <= 0.3f) _magText.color = new Color(1f, 0.6f, 0.2f);
                else _magText.color = Color.white;
            }
            if (_reserveText != null) _reserveText.text = "/ " + reserve;
            RefreshWeaponName();
        }

        private void HandleReloadStarted(float duration)
        {
            _reloading = true;
            _reloadDuration = duration;
            _reloadStartTime = Time.time;
            if (_reloadText != null)
            {
                _reloadText.transform.parent.gameObject.SetActive(true);
                if (_reloadBarFill != null) _reloadBarFill.anchorMax = new Vector2(0f, 1f);
            }
        }

        private void RefreshWeaponName()
        {
            if (_weaponNameText == null || _weaponManager == null) return;
            var weapon = _weaponManager.CurrentWeapon;
            _weaponNameText.text = weapon != null ? weapon.DisplayName : "";
        }

        private void UpdateProtectionIndicator()
        {
            if (_protectionText == null) return;
            bool protectedNow = _localHealth != null && _localHealth.IsSpawnProtected;
            _protectionText.gameObject.SetActive(protectedNow);
            if (!protectedNow) return;
            float pulse = 0.55f + Mathf.PingPong(Time.time * 3.5f, 0.45f);
            _protectionText.color = Color.Lerp(new Color(0.45f, 0.9f, 1f), Color.white, pulse);
        }

        private void BuildUI()
        {
            var canvasObj = new GameObject("BattleAmmoCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 501;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.transform.SetParent(transform, false);

            var panel = MakeRect(canvasObj.transform, "AmmoPanel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f));
            panel.anchoredPosition = new Vector2(-120f, 80f);
            panel.sizeDelta = new Vector2(220f, 100f);
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.45f);
            panelImg.raycastTarget = false;

            _magText = MakeText(panel, "MagText", 48, FontStyle.Bold, TextAnchor.MiddleRight);
            var magRect = _magText.GetComponent<RectTransform>();
            magRect.anchorMin = new Vector2(0f, 0.25f);
            magRect.anchorMax = new Vector2(0.55f, 1f);
            magRect.offsetMin = new Vector2(8f, 0f);
            magRect.offsetMax = new Vector2(-4f, -4f);

            _reserveText = MakeText(panel, "ReserveText", 24, FontStyle.Normal, TextAnchor.LowerLeft);
            var reserveRect = _reserveText.GetComponent<RectTransform>();
            reserveRect.anchorMin = new Vector2(0.57f, 0.25f);
            reserveRect.anchorMax = new Vector2(1f, 0.75f);
            reserveRect.offsetMin = Vector2.zero;
            reserveRect.offsetMax = new Vector2(-8f, 0f);
            _reserveText.color = new Color(0.75f, 0.75f, 0.75f);

            _weaponNameText = MakeText(panel, "WeaponName", 16, FontStyle.Normal, TextAnchor.MiddleCenter);
            var nameRect = _weaponNameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0.28f);
            nameRect.offsetMin = new Vector2(4f, 2f);
            nameRect.offsetMax = new Vector2(-4f, 0f);
            _weaponNameText.color = new Color(0.85f, 0.65f, 0.3f);

            var reloadGroup = new GameObject("ReloadGroup", typeof(RectTransform));
            reloadGroup.transform.SetParent(canvasObj.transform, false);
            var reloadGroupRect = reloadGroup.GetComponent<RectTransform>();
            reloadGroupRect.anchorMin = new Vector2(1f, 0f);
            reloadGroupRect.anchorMax = new Vector2(1f, 0f);
            reloadGroupRect.pivot = new Vector2(0.5f, 1f);
            reloadGroupRect.anchoredPosition = new Vector2(-120f, 22f);
            reloadGroupRect.sizeDelta = new Vector2(220f, 40f);

            _reloadText = MakeText(reloadGroupRect, "ReloadLabel", 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            var reloadTextRect = _reloadText.GetComponent<RectTransform>();
            reloadTextRect.anchorMin = new Vector2(0f, 0.5f);
            reloadTextRect.anchorMax = Vector2.one;
            reloadTextRect.offsetMin = Vector2.zero;
            reloadTextRect.offsetMax = Vector2.zero;
            _reloadText.text = "RELOADING";
            _reloadText.color = new Color(1f, 0.85f, 0.3f);

            var barBg = MakeRect(reloadGroupRect, "BarBg", Vector2.zero, new Vector2(1f, 0.45f), new Vector2(0.5f, 0.5f));
            barBg.offsetMin = new Vector2(10f, 2f);
            barBg.offsetMax = new Vector2(-10f, -2f);
            var barBgImg = barBg.gameObject.AddComponent<Image>();
            barBgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            barBgImg.raycastTarget = false;

            var barFill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg, false);
            _reloadBarFill = barFill.GetComponent<RectTransform>();
            _reloadBarFill.anchorMin = Vector2.zero;
            _reloadBarFill.anchorMax = new Vector2(0f, 1f);
            _reloadBarFill.offsetMin = Vector2.zero;
            _reloadBarFill.offsetMax = Vector2.zero;
            _reloadBar = barFill.GetComponent<Image>();
            _reloadBar.color = new Color(1f, 0.75f, 0.2f, 0.9f);
            _reloadBar.raycastTarget = false;

            reloadGroup.SetActive(false);

            var hpPanel = MakeRect(canvasObj.transform, "HpPanel", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f));
            hpPanel.anchoredPosition = new Vector2(130f, 80f);
            hpPanel.sizeDelta = new Vector2(240f, 55f);
            var hpPanelImg = hpPanel.gameObject.AddComponent<Image>();
            hpPanelImg.color = new Color(0f, 0f, 0f, 0.45f);
            hpPanelImg.raycastTarget = false;

            var hpLabel = MakeText(hpPanel, "HpLabel", 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            var hpLabelRt = hpLabel.GetComponent<RectTransform>();
            hpLabelRt.anchorMin = new Vector2(0f, 0.6f);
            hpLabelRt.anchorMax = new Vector2(1f, 1f);
            hpLabelRt.offsetMin = new Vector2(10f, 0f);
            hpLabelRt.offsetMax = new Vector2(-10f, -2f);
            hpLabel.text = "HP";
            hpLabel.color = new Color(0.7f, 0.7f, 0.7f);

            _hpText = MakeText(hpPanel, "HpValue", 18, FontStyle.Bold, TextAnchor.MiddleRight);
            var hpValRt = _hpText.GetComponent<RectTransform>();
            hpValRt.anchorMin = new Vector2(0f, 0.6f);
            hpValRt.anchorMax = new Vector2(1f, 1f);
            hpValRt.offsetMin = new Vector2(10f, 0f);
            hpValRt.offsetMax = new Vector2(-10f, -2f);

            var hpBarBg = MakeRect(hpPanel, "HpBarBg", new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f));
            hpBarBg.offsetMin = new Vector2(8f, 6f);
            hpBarBg.offsetMax = new Vector2(-8f, -2f);
            var hpBarBgImg = hpBarBg.gameObject.AddComponent<Image>();
            hpBarBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            hpBarBgImg.raycastTarget = false;

            var hpFillObj = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
            hpFillObj.transform.SetParent(hpBarBg, false);
            var hpFillRt = hpFillObj.GetComponent<RectTransform>();
            hpFillRt.anchorMin = Vector2.zero;
            hpFillRt.anchorMax = Vector2.one;
            hpFillRt.offsetMin = Vector2.zero;
            hpFillRt.offsetMax = Vector2.zero;
            hpFillRt.pivot = new Vector2(0f, 0.5f);
            _hpFill = hpFillObj.GetComponent<Image>();
            _hpFill.color = new Color(0.2f, 0.85f, 0.2f);
            _hpFill.raycastTarget = false;

            _protectionText = MakeText(canvasObj.transform, "ProtectionText", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            var protectionRt = _protectionText.GetComponent<RectTransform>();
            protectionRt.anchorMin = new Vector2(0.5f, 0f);
            protectionRt.anchorMax = new Vector2(0.5f, 0f);
            protectionRt.pivot = new Vector2(0.5f, 0f);
            protectionRt.anchoredPosition = new Vector2(0f, 138f);
            protectionRt.sizeDelta = new Vector2(320f, 32f);
            _protectionText.text = "SPAWN SHIELD";
            _protectionText.color = new Color(0.45f, 0.9f, 1f);
            _protectionText.gameObject.SetActive(false);
        }

        private static RectTransform MakeRect(Transform parent, string n, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var obj = new GameObject(n, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            return rt;
        }

        private static Text MakeText(Transform parent, string n, int fontSize, FontStyle style, TextAnchor alignment)
        {
            var obj = new GameObject(n, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var txt = obj.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = alignment;
            txt.color = Color.white;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }
    }
}
