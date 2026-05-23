using UnityEngine;
using _TeamFolder.JCJ.Script;

// 발사체 이동, 충돌, 피해 적용을 처리하는 런타임 로직.

namespace _TeamFolder.JCJ.Battle
{
    public class BattleProjectile : MonoBehaviour
        , IBattlePoolAware
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Vector3 _velocity;
        private float _gravity;
        private float _damage;
        private float _radius;
        private float _lifeRemaining;
        private LayerMask _hitMask;
        private GameObject _owner;
        private bool _launched;
        private GameObject _impactEffectPrefab;
        private AudioClip _impactSfx;
        private Color _projectileColor = new(1f, 0.78f, 0.25f, 1f);
        private float _projectileVisualScale = 0.08f;
        private string _weaponId = string.Empty;
        private Transform _visualRoot;
        private Renderer _visualRenderer;
        private TrailRenderer _trailRenderer;
        private Material _runtimeMaterial;
        public static event System.Action<BattleDamageInfo> DamageReported;

        private void Awake()
        {
            EnsureFallbackVisuals();
            RefreshPresentation();
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }

        public void ApplyPresentation(BattleWeaponDefinition definition)
        {
            if (definition == null) return;

            _impactEffectPrefab = definition.ImpactEffectPrefab;
            _impactSfx = definition.ImpactSfx;
            _projectileColor = definition.ProjectileColor;
            _projectileVisualScale = definition.ProjectileVisualScale;
            _weaponId = definition.WeaponId;

            EnsureFallbackVisuals();
            RefreshPresentation();
        }

        public void Launch(GameObject owner, Vector3 direction, float speed, float gravity, float damage, float radius, float lifetime, LayerMask hitMask)
        {
            // 현재는 발사체가 로컬에서 바로 생성되고 비주얼도 같은 오브젝트가 담당한다.
            // 서버 권위 구조에서는 owner, 시작 위치, 방향, 발사 시각을 서버 기준으로 고정해 재현해야 판정 차이가 줄어든다.
            _owner = owner;
            _velocity = direction.normalized * speed;
            _gravity = gravity;
            _damage = damage;
            _radius = radius;
            _lifeRemaining = lifetime;
            _hitMask = hitMask.value == 0 ? Physics.DefaultRaycastLayers : hitMask;
            _launched = true;
            transform.forward = _velocity.normalized;
            EnsureFallbackVisuals();
            RefreshPresentation();
        }

        // 발사체 이동과 충돌 검사를 매 물리 틱마다 진행한다.
        // 서버에서만 판정하고 클라이언트는 위치만 보간하는 구조로 바꿀 때 핵심 교체 지점이다.
        private void FixedUpdate()
        {
            if (!_launched) return;

            float dt = Time.fixedDeltaTime;
            Vector3 start = transform.position;
            Vector3 nextVelocity = _velocity + Vector3.up * (_gravity * dt);
            Vector3 travel = (_velocity + nextVelocity) * 0.5f * dt;
            float distance = travel.magnitude;

            if (distance > 0f)
            {
                if (Physics.SphereCast(start, _radius, travel.normalized, out var hit, distance, _hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (!IsOwnerHit(hit.collider))
                    {
                        ApplyHit(hit);
                        return;
                    }
                }

                transform.position = start + travel;
                if (nextVelocity.sqrMagnitude > 0.0001f) transform.forward = nextVelocity.normalized;
            }

            _velocity = nextVelocity;
            _lifeRemaining -= dt;
            if (_lifeRemaining <= 0f) Release();
        }

        private bool IsOwnerHit(Collider hitCollider)
        {
            if (_owner == null || hitCollider == null) return false;
            return hitCollider.transform.root == _owner.transform.root;
        }

        // 충돌 후 피해 계산, 헤드샷 판정, 이펙트 재생을 한 번에 처리한다.
        // 서버 연동 시에는 데미지 확정과 킬 판정은 서버에서 끝내고, 여기서는 그 결과를 재생하는 쪽으로 분리하기 쉽다.
        private void ApplyHit(RaycastHit hit)
        {
            var damageable = hit.collider.GetComponentInParent<IBattleDamageable>();
            var battleHealth = hit.collider.GetComponentInParent<BattleHealth>();
            var targetIdentity = RuntimePlayerIdentity.Find(hit.collider);
            var attackerIdentity = RuntimePlayerIdentity.Find(_owner != null ? _owner.transform : null);
            bool isHeadshot = hit.collider.gameObject.name.Contains("Head");
            float finalDamage = isHeadshot ? _damage * 1.5f : _damage;
            bool damageApplied = false;

            var damageInfo = new BattleDamageInfo
            {
                AttackerId = attackerIdentity != null ? attackerIdentity.PlayerId : (_owner != null ? _owner.name : string.Empty),
                AttackerDisplayName = attackerIdentity != null ? attackerIdentity.DisplayName : (_owner != null ? _owner.name : string.Empty),
                TargetId = targetIdentity != null ? targetIdentity.PlayerId : (battleHealth != null ? battleHealth.gameObject.name : hit.collider.gameObject.name),
                TargetDisplayName = targetIdentity != null ? targetIdentity.DisplayName : (battleHealth != null ? battleHealth.gameObject.name : hit.collider.gameObject.name),
                WeaponId = _weaponId,
                Attacker = _owner,
                Target = battleHealth != null ? battleHealth.gameObject : hit.collider.gameObject,
                Projectile = gameObject,
                HitPoint = hit.point,
                HitNormal = hit.normal,
                Damage = finalDamage,
                IsHeadshot = isHeadshot
            };

            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                DamageReported?.Invoke(damageInfo);
            }
            else if (damageable != null)
            {
                if (battleHealth != null) damageApplied = battleHealth.TryApplyDamage(damageInfo);
                else
                {
                    damageable.ApplyDamage(damageInfo);
                    damageApplied = true;
                }
            }

            if (damageApplied)
            {
                BattleFirstPersonCamera.ShowHitMarker(isHeadshot);
                SpawnDamagePopup(hit.point, finalDamage, isHeadshot);
                if (isHeadshot) SpawnBloodBurst(hit.point, hit.normal);
            }

            SpawnImpactFeedback(hit.point, hit.normal);
            Release();
        }

        private void SpawnBloodBurst(Vector3 position, Vector3 normal)
        {
            var bloodObj = new GameObject("BloodBurst");
            bloodObj.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal));

            var ps = bloodObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.05f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.7f, 0f, 0f), new Color(1f, 0.1f, 0.1f));
            main.gravityModifier = 4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 18) });
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.05f;

            var psr = bloodObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null) psr.material = CreateParticleMaterial();

            ps.Play();
            Destroy(bloodObj, 0.6f);
        }

        private void SpawnDamagePopup(Vector3 position, float damage, bool headshot)
        {
            var popupObj = BattlePoolManager.Spawn(
                "battle.damage_popup",
                () =>
                {
                    var popupRoot = new GameObject("DmgPopup");
                    popupRoot.AddComponent<BattleDamagePopup>();
                    return popupRoot;
                },
                position + Vector3.up * 0.3f,
                Quaternion.identity);
            var popup = popupObj.GetComponent<BattleDamagePopup>();
            if (popup != null) popup.Initialize(Mathf.RoundToInt(damage), headshot);
        }

        private void EnsureFallbackVisuals()
        {
            if (_trailRenderer == null) _trailRenderer = GetComponent<TrailRenderer>();
            if (_trailRenderer != null) _trailRenderer.enabled = false;

            if (_visualRenderer != null)
            {
                _visualRenderer.enabled = false;
                return;
            }

            var existingRenderer = GetComponentInChildren<Renderer>(true);
            if (existingRenderer != null && !(existingRenderer is TrailRenderer))
            {
                _visualRenderer = existingRenderer;
                _visualRoot = existingRenderer.transform;
                _visualRenderer.enabled = false;
                return;
            }
        }

        private void RefreshPresentation()
        {
            if (_visualRoot != null)
            {
                _visualRoot.localScale = Vector3.zero;
            }

            var material = GetRuntimeMaterial();
            material.color = _projectileColor;
            if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, _projectileColor);
            if (material.HasProperty(ColorId)) material.SetColor(ColorId, _projectileColor);

            if (_trailRenderer != null) _trailRenderer.enabled = false;

            if (_visualRenderer == null) return;

            _visualRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _visualRenderer.receiveShadows = false;
            _visualRenderer.sharedMaterial = material;
            _visualRenderer.enabled = false;
        }

        private Material GetRuntimeMaterial()
        {
            if (_runtimeMaterial != null) return _runtimeMaterial;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("UI/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Hidden/InternalColored");

            _runtimeMaterial = new Material(shader);
            _runtimeMaterial.color = _projectileColor;
            if (_runtimeMaterial.HasProperty(BaseColorId)) _runtimeMaterial.SetColor(BaseColorId, _projectileColor);
            if (_runtimeMaterial.HasProperty(ColorId)) _runtimeMaterial.SetColor(ColorId, _projectileColor);
            _runtimeMaterial.renderQueue = 3100;

            return _runtimeMaterial;
        }

        private void SpawnImpactFeedback(Vector3 position, Vector3 normal)
        {
            if (_impactEffectPrefab != null)
            {
                var spawnedFx = Instantiate((Object)_impactEffectPrefab, position, Quaternion.LookRotation(normal));
                float impactScale = GetImpactScale();
                if (spawnedFx is GameObject fxObject)
                {
                    fxObject.transform.localScale = Vector3.one * impactScale;
                    Destroy(fxObject, 2f);
                }
                else if (spawnedFx is Component fxComponent)
                {
                    fxComponent.transform.localScale = Vector3.one * impactScale;
                    Destroy(fxComponent.gameObject, 2f);
                }
            }
            else
            {
                SpawnProceduralImpact(position, normal);
            }

            if (_impactSfx != null) AudioSource.PlayClipAtPoint(_impactSfx, position, 0.9f);
        }

        private void SpawnProceduralImpact(Vector3 position, Vector3 normal)
        {
            var impactRoot = new GameObject("BattleImpactFx");
            impactRoot.transform.SetPositionAndRotation(position + normal * 0.01f, Quaternion.LookRotation(normal));
            float impactScale = GetImpactScale();
            impactRoot.transform.localScale = Vector3.one * impactScale;

            CreateImpactSparks(impactRoot.transform);

            var impactLight = impactRoot.AddComponent<Light>();
            impactLight.type = LightType.Point;
            impactLight.color = _projectileColor;
            impactLight.intensity = Mathf.Lerp(0.7f, 2.2f, Mathf.InverseLerp(0.024f, 0.145f, _projectileVisualScale));
            impactLight.range = Mathf.Lerp(0.8f, 2.4f, Mathf.InverseLerp(0.024f, 0.145f, _projectileVisualScale));
            impactLight.shadows = LightShadows.None;
            impactRoot.AddComponent<BattleMuzzleFlashFade>();

            Destroy(impactRoot, 0.8f);
        }

        private void CreateImpactSparks(Transform parent)
        {
            var sparkObj = new GameObject("ImpactSparks");
            sparkObj.transform.SetParent(parent, false);

            var ps = sparkObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.025f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(_projectileColor.r, _projectileColor.g, _projectileColor.b, 1f),
                new Color(1f, 0.85f, 0.5f, 1f));
            main.gravityModifier = 3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 15;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 12) });
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.01f;

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(_projectileColor, 0.5f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = gradient;

            var psr = sparkObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null) psr.material = CreateParticleMaterial();

            ps.Play();
        }

        private Material CreateParticleMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("UI/Default");

            var mat = new Material(shader);
            mat.color = _projectileColor;
            if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, _projectileColor);
            if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, _projectileColor);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.renderQueue = 3100;
            return mat;
        }

        private float GetImpactScale()
        {
            float baseScale = Mathf.Max(_projectileVisualScale / 0.07f, _radius / 0.04f);
            return Mathf.Clamp(baseScale, 0.55f, 1.9f);
        }

        public void OnSpawnedFromPool()
        {
            _launched = false;
            _lifeRemaining = 0f;
            _velocity = Vector3.zero;
            if (_trailRenderer != null) _trailRenderer.Clear();
        }

        public void OnReturnedToPool()
        {
            _launched = false;
            _lifeRemaining = 0f;
            _velocity = Vector3.zero;
            _owner = null;
            if (_trailRenderer != null) _trailRenderer.Clear();
        }

        private void Release()
        {
            BattlePoolManager.Release(gameObject);
        }
    }
}
