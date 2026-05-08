using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// 체력, 피격 반응, 사망 상태를 관리하는 컴포넌트.

namespace _TeamFolder.JCJ.Battle
{
    public class BattleHealth : MonoBehaviour, IBattleDamageable
    {
        [SerializeField] private float _maxHealth = 175f;
        [SerializeField] private bool _disableOnDeath = true;
        [SerializeField] private float _hitFlashDuration = 0.12f;
        [SerializeField] private Color _hitFlashColor = new(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _spawnProtectionColor = new(0.35f, 0.95f, 1f, 1f);

        private readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private readonly int _colorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _block;
        private Renderer[] _renderers;
        private Color[] _originalColors;
        private Coroutine _flashRoutine;
        private Image _healthFill;
        private Canvas _healthCanvas;
        private Text _statusText;
        private int _teamIndex = -1;
        private float _spawnProtectedUntil;
        private Transform _spawnShield;
        private Renderer _spawnShieldRenderer;
        private Material _spawnShieldMaterial;

        public float MaxHealth => _maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public bool HideHealthBar { get; set; }
        public int TeamIndex => _teamIndex;
        public bool IsSpawnProtected => Time.time < _spawnProtectedUntil;
        public event System.Action<BattleHealth, BattleDamageInfo> Died;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            CurrentHealth = Mathf.Max(1f, _maxHealth);
            CacheRenderers();
            CreateSpawnShield();
        }

        private void Start()
        {
            if (!HideHealthBar) CreateWorldHealthBar();
        }

        private void LateUpdate()
        {
            if (_healthCanvas != null && Camera.main != null)
                _healthCanvas.transform.rotation =
                    Quaternion.LookRotation(_healthCanvas.transform.position - Camera.main.transform.position);
            UpdateSpawnProtectionVisual();
        }

        public void ApplyDamage(BattleDamageInfo damageInfo)
        {
            TryApplyDamage(damageInfo);
        }

        public bool ApplyAuthoritativeDamage(BattleDamageInfo damageInfo)
        {
            return TryApplyDamage(damageInfo);
        }

        // 실제 체력 차감과 사망 이벤트 발생 지점이다.
        // 서버 권위 구조에서는 이 메서드가 로컬 검증보다 서버 확정 피해를 반영하는 곳으로 쓰기 좋다.
        public bool TryApplyDamage(BattleDamageInfo damageInfo)
        {
            if (IsDead) return false;
            if (IsFriendlyFire(damageInfo.Attacker)) return false;
            if (IsSpawnProtected) return false;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, damageInfo.Damage));
            UpdateHealthBar();

            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());

            if (CurrentHealth <= 0f)
            {
                IsDead = true;
                Died?.Invoke(this, damageInfo);
                if (_disableOnDeath) gameObject.SetActive(false);
            }

            return true;
        }

        public void SetBaseTint(Color color)
        {
            if (_renderers == null || _renderers.Length == 0) CacheRenderers();
            if (_originalColors == null) return;
            for (int i = 0; i < _originalColors.Length; i++)
                _originalColors[i] = color;
            if (_flashRoutine == null) RestoreRendererColor();
        }

        public void SetTeamIndex(int teamIndex)
        {
            _teamIndex = teamIndex;
        }

        // 리스폰 직후 일정 시간 피해를 막는 상태를 설정한다.
        // 서버를 붙이면 무적 종료 시각도 서버 시간 기준으로 맞추는 편이 안전하다.
        public void ActivateSpawnProtection(float duration)
        {
            if (duration <= 0f) return;
            _spawnProtectedUntil = Mathf.Max(_spawnProtectedUntil, Time.time + duration);
            UpdateStatusIndicator();
        }

        // 사망 후 체력, 상태, UI를 기본값으로 되돌린다.
        // 리스폰 확정 이벤트를 받은 뒤 호출하는 복구 단계로 이해하면 된다.
        public void ResetForRespawn()
        {
            IsDead = false;
            CurrentHealth = Mathf.Max(1f, _maxHealth);
            UpdateHealthBar();
            StopHitFlash();
            UpdateStatusIndicator();
        }

        private void CreateWorldHealthBar()
        {
            var holder = new GameObject("HealthBarHolder");
            holder.transform.SetParent(transform, false);
            holder.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            var canvasObj = new GameObject("HealthCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObj.transform.SetParent(holder.transform, false);
            _healthCanvas = canvasObj.GetComponent<Canvas>();
            _healthCanvas.renderMode = RenderMode.WorldSpace;
            _healthCanvas.sortingOrder = 550;

            var canvasRt = canvasObj.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(120f, 14f);
            canvasRt.localScale = Vector3.one * 0.008f;

            var bgObj = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgObj.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.75f);
            bgObj.GetComponent<Image>().raycastTarget = false;

            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(bgObj.transform, false);
            var fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(1f, 1f);
            fillRt.offsetMax = new Vector2(-1f, -1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            _healthFill = fillObj.GetComponent<Image>();
            _healthFill.color = new Color(0.2f, 0.85f, 0.2f);
            _healthFill.raycastTarget = false;

            var statusObj = new GameObject("Status", typeof(RectTransform), typeof(Text));
            statusObj.transform.SetParent(canvasObj.transform, false);
            var statusRt = statusObj.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0f, 1f);
            statusRt.anchorMax = new Vector2(1f, 1f);
            statusRt.offsetMin = new Vector2(0f, 14f);
            statusRt.offsetMax = new Vector2(0f, 34f);
            _statusText = statusObj.GetComponent<Text>();
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_statusText.font == null) _statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _statusText.fontSize = 18;
            _statusText.fontStyle = FontStyle.Bold;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = _spawnProtectionColor;
            _statusText.raycastTarget = false;
            _statusText.text = string.Empty;
        }

        private void CreateSpawnShield()
        {
            var shieldObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldObject.name = "SpawnShield";
            shieldObject.transform.SetParent(transform, false);
            shieldObject.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            shieldObject.transform.localScale = Vector3.one * 2.8f;
            var collider = shieldObject.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            _spawnShieldRenderer = shieldObject.GetComponent<Renderer>();
            if (_spawnShieldRenderer != null)
            {
                _spawnShieldRenderer.shadowCastingMode = ShadowCastingMode.Off;
                _spawnShieldRenderer.receiveShadows = false;
                _spawnShieldRenderer.lightProbeUsage = LightProbeUsage.Off;
                _spawnShieldRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                _spawnShieldMaterial = CreateSpawnShieldMaterial();
                _spawnShieldRenderer.sharedMaterial = _spawnShieldMaterial;
            }
            _spawnShield = shieldObject.transform;
            shieldObject.SetActive(false);
        }

        private void UpdateHealthBar()
        {
            if (_healthFill == null) return;
            float ratio = Mathf.Clamp01(CurrentHealth / _maxHealth);
            _healthFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            if (ratio > 0.5f) _healthFill.color = new Color(0.2f, 0.85f, 0.2f);
            else if (ratio > 0.25f) _healthFill.color = new Color(0.9f, 0.7f, 0.1f);
            else _healthFill.color = new Color(0.9f, 0.15f, 0.15f);
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    _originalColors[i] = Color.white;
                    continue;
                }

                if (renderer.sharedMaterial.HasProperty(_baseColorId)) _originalColors[i] = renderer.sharedMaterial.GetColor(_baseColorId);
                else if (renderer.sharedMaterial.HasProperty(_colorId)) _originalColors[i] = renderer.sharedMaterial.GetColor(_colorId);
                else _originalColors[i] = Color.white;
            }
        }

        private IEnumerator FlashRoutine()
        {
            SetRendererColor(_hitFlashColor);
            yield return new WaitForSeconds(_hitFlashDuration);
            RestoreRendererColorImmediate();
            _flashRoutine = null;
        }

        private void SetRendererColor(Color color)
        {
            if (_renderers == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block);
                _block.SetColor(_baseColorId, color);
                _block.SetColor(_colorId, color);
                renderer.SetPropertyBlock(_block);
            }
        }

        private void RestoreRendererColor()
        {
            if (_flashRoutine != null) return;
            RestoreRendererColorImmediate();
        }

        private void RestoreRendererColorImmediate()
        {
            if (_renderers == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block);
                _block.SetColor(_baseColorId, _originalColors[i]);
                _block.SetColor(_colorId, _originalColors[i]);
                renderer.SetPropertyBlock(_block);
            }
        }

        private void UpdateSpawnProtectionVisual()
        {
            UpdateStatusIndicator();
            if (_flashRoutine != null || _renderers == null) return;

            if (!IsSpawnProtected)
            {
                if (_spawnShield != null && _spawnShield.gameObject.activeSelf) _spawnShield.gameObject.SetActive(false);
                RestoreRendererColorImmediate();
                return;
            }

            float pulse = 0.5f + Mathf.PingPong(Time.time * 4f, 0.5f);
            Color tint = Color.Lerp(_originalColors != null && _originalColors.Length > 0 ? _originalColors[0] : Color.white, _spawnProtectionColor, pulse);
            SetRendererColor(tint);
            UpdateSpawnShield(pulse);
        }

        private void UpdateSpawnShield(float pulse)
        {
            if (_spawnShield == null || _spawnShieldMaterial == null) return;
            if (!_spawnShield.gameObject.activeSelf) _spawnShield.gameObject.SetActive(true);
            float scale = Mathf.Lerp(2.65f, 2.9f, pulse);
            _spawnShield.localScale = Vector3.one * scale;
            var shieldColor = Color.Lerp(new Color(_spawnProtectionColor.r, _spawnProtectionColor.g, _spawnProtectionColor.b, 0.1f),
                new Color(_spawnProtectionColor.r, _spawnProtectionColor.g, _spawnProtectionColor.b, 0.2f), pulse);
            if (_spawnShieldMaterial.HasProperty(_baseColorId)) _spawnShieldMaterial.SetColor(_baseColorId, shieldColor);
            if (_spawnShieldMaterial.HasProperty(_colorId)) _spawnShieldMaterial.SetColor(_colorId, shieldColor);
        }

        private void UpdateStatusIndicator()
        {
            if (_statusText == null) return;
            _statusText.text = IsSpawnProtected ? "SPAWN SHIELD" : string.Empty;
        }

        private void StopHitFlash()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }
            RestoreRendererColorImmediate();
        }

        private Material CreateSpawnShieldMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var material = new Material(shader);
            var color = new Color(_spawnProtectionColor.r, _spawnProtectionColor.g, _spawnProtectionColor.b, 0.14f);
            if (material.HasProperty(_baseColorId)) material.SetColor(_baseColorId, color);
            if (material.HasProperty(_colorId)) material.SetColor(_colorId, color);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
            material.renderQueue = 3000;
            return material;
        }

        private bool IsFriendlyFire(GameObject attacker)
        {
            if (_teamIndex < 0 || attacker == null) return false;
            var attackerHealth = attacker.GetComponentInChildren<BattleHealth>();
            if (attackerHealth == null) attackerHealth = attacker.GetComponentInParent<BattleHealth>();
            if (attackerHealth == null) return false;
            return attackerHealth.TeamIndex >= 0 && attackerHealth.TeamIndex == _teamIndex;
        }

    }
}
