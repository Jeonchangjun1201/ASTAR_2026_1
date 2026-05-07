using UnityEngine;
using UnityEngine.InputSystem;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Battle
{
    public class BattleWeaponManager : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Vector3 DefaultWeaponMountLocalPosition = new(0.06f, -0.08f, 0.12f);
        private const string RuntimeProjectilePoolKey = "battle.runtime_projectile";
        private const string TracerPoolKey = "battle.tracer";

        [SerializeField] private BattleWeaponCatalog _weaponCatalog;
        [SerializeField] private Transform _cameraRoot;
        [SerializeField] private Transform _weaponMount;
        [SerializeField] private LayerMask _hitMask = ~0;
        [SerializeField] private bool _isLocalControlled;
        [SerializeField] private bool _autoCreateMount = true;
        [SerializeField] private BattleWeaponDefinition _startingWeapon;
        [SerializeField] private int _startingRank = 4;

        private InputActionMap _inputMap;
        private InputAction _fireAction;
        private InputAction _reloadAction;
        private InputAction _aimAction;
        private BattleWeaponDefinition _currentWeapon;
        private GameObject _weaponInstance;
        private Transform _muzzle;
        private float _nextFireTime;

        private int _currentMagazine;
        private int _reserveAmmo;
        private bool _isReloading;
        private float _reloadFinishTime;
        private bool _autoEquipOnStart = true;
        private bool _inputEnabled = true;

        public BattleWeaponDefinition CurrentWeapon => _currentWeapon;
        public int CurrentMagazine => _currentMagazine;
        public int ReserveAmmo => _reserveAmmo;
        public bool IsReloading => _isReloading;

        public event System.Action<int, int, int> OnAmmoChanged;
        public event System.Action<float> OnReloadStarted;

        private void Awake()
        {
            ResolveRig();
            BuildInput();
        }

        private void Start()
        {
            if (!_autoEquipOnStart) return;
            if (_currentWeapon != null) return;
            if (_startingWeapon != null && _weaponCatalog == null)
            {
                EquipWeapon(_startingWeapon);
            }
            else if (_weaponCatalog != null)
            {
                EquipRandomWeaponForRank(_startingRank);
            }
        }

        private void OnEnable()
        {
            if (_isLocalControlled) _inputMap?.Enable();
        }

        private void OnDisable()
        {
            _inputMap?.Disable();
        }

        private void OnDestroy()
        {
            _inputMap?.Disable();
            _inputMap?.Dispose();
        }

        private void Update()
        {
            if (!_isLocalControlled) return;

            bool aiming = _inputEnabled && _aimAction != null && _aimAction.IsPressed();
            var cam = BattleFirstPersonCamera.Instance;
            if (cam != null) cam.SetAiming(aiming);

            if (!_inputEnabled)
            {
                if (_isReloading && Time.time >= _reloadFinishTime) FinishReload();
                return;
            }

            if (_isReloading)
            {
                if (Time.time >= _reloadFinishTime) FinishReload();
                return;
            }

            HandleWeaponSwitch();

            if (_currentWeapon == null || _fireAction == null) return;

            if (_reloadAction != null && _reloadAction.WasPressedThisFrame()) { TryReload(); return; }

            bool wantsFire = _currentWeapon.Automatic ? _fireAction.IsPressed() : _fireAction.WasPressedThisFrame();
            if (!wantsFire) return;

            if (_currentMagazine <= 0) { TryReload(); return; }

            TryFire();
        }

        private void HandleWeaponSwitch()
        {
            if (_weaponCatalog == null) return;
            int rank = -1;
            bool preferCompactWeapon = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) rank = 1;
                else if (Keyboard.current.digit2Key.wasPressedThisFrame) rank = 2;
                else if (Keyboard.current.digit3Key.wasPressedThisFrame) rank = 3;
                else if (Keyboard.current.digit4Key.wasPressedThisFrame)
                {
                    rank = 4;
                    preferCompactWeapon = true;
                }
                else if (Keyboard.current.qKey.wasPressedThisFrame) rank = _startingRank;
            }
            if (rank < 0) return;
            _startingRank = rank;
            if (preferCompactWeapon) EquipCompactWeaponForRank(rank);
            else EquipRandomWeaponForRank(rank);
        }

        public void SetLocalControlled(bool isLocalControlled)
        {
            // 서버 연결 후에는 무기 입력 소유권도 플레이어 소유권과 같은 기준으로 움직여야 한다.
            // 원격 플레이어 무기는 발사 입력을 직접 읽지 않고 동기화된 결과만 재생해야 한다.
            bool ownershipChanged = _isLocalControlled != isLocalControlled;
            _isLocalControlled = isLocalControlled;
            if (_inputMap == null) return;
            if (_isLocalControlled) _inputMap.Enable();
            else _inputMap.Disable();
            if (ownershipChanged) RebuildOwnerPresentation();
        }

        public void Configure(BattleWeaponCatalog weaponCatalog, bool isLocalControlled, int rank)
        {
            _weaponCatalog = weaponCatalog;
            _startingRank = rank;
            SetLocalControlled(isLocalControlled);
        }

        public void SetAutoEquipOnStart(bool autoEquipOnStart)
        {
            _autoEquipOnStart = autoEquipOnStart;
        }

        public void SetInputEnabled(bool inputEnabled)
        {
            _inputEnabled = inputEnabled;
            if (!_inputEnabled)
            {
                var cam = BattleFirstPersonCamera.Instance;
                if (cam != null) cam.SetAiming(false);
            }
        }

        public void EquipRandomWeaponForRank(int rank)
        {
            if (_weaponCatalog == null) return;
            var definition = _weaponCatalog.GetRandomWeapon(BattleWeaponCatalog.RankToGrade(rank));
            if (definition != null) EquipWeapon(definition);
        }

        public void EquipCompactWeaponForRank(int rank)
        {
            if (_weaponCatalog == null)
            {
                EquipRandomWeaponForRank(rank);
                return;
            }

            BattleWeaponDefinition[] compactCandidates = null;
            var targetGrade = BattleWeaponCatalog.RankToGrade(rank);
            var loadouts = _weaponCatalog.Loadouts;
            for (int i = 0; i < loadouts.Count; i++)
            {
                var loadout = loadouts[i];
                if (loadout == null || loadout.grade != targetGrade || loadout.weapons == null) continue;
                var filtered = new System.Collections.Generic.List<BattleWeaponDefinition>(loadout.weapons.Length);
                for (int j = 0; j < loadout.weapons.Length; j++)
                {
                    var weapon = loadout.weapons[j];
                    if (weapon == null || weapon.ViewPrefab == null) continue;
                    var prefabName = weapon.ViewPrefab.name;
                    if (!string.IsNullOrEmpty(prefabName) && prefabName.Contains("Handgun")) filtered.Add(weapon);
                }
                compactCandidates = filtered.ToArray();
                break;
            }

            if (compactCandidates == null || compactCandidates.Length == 0)
            {
                EquipRandomWeaponForRank(rank);
                return;
            }

            var definition = compactCandidates[Random.Range(0, compactCandidates.Length)];
            if (definition != null) EquipWeapon(definition);
        }

        public void EquipWeapon(BattleWeaponDefinition definition)
        {
            _currentWeapon = definition;
            _nextFireTime = 0f;
            _isReloading = false;

            if (definition != null)
            {
                _currentMagazine = definition.MagazineSize;
                _reserveAmmo = definition.TotalAmmo - definition.MagazineSize;
                NotifyAmmoChanged();
            }

            if (_weaponInstance != null) Destroy(_weaponInstance);
            _weaponInstance = null;
            _muzzle = null;

            // 서버 게임 기준으로 1인칭 무기 실물은 현재 오너 클라이언트만 생성한다.
            // 원격 플레이어는 서버 동기화 결과(발사, 피격, 위치)만 재생하고 로컬 입력용 뷰는 만들지 않는다.
            if (!_isLocalControlled) return;
            if (definition == null || _weaponMount == null || definition.ViewPrefab == null) return;

            var spawnedView = Instantiate((Object)definition.ViewPrefab, _weaponMount.position, _weaponMount.rotation, _weaponMount);
            if (spawnedView is GameObject viewGameObject) _weaponInstance = viewGameObject;
            else if (spawnedView is Component viewComponent) _weaponInstance = viewComponent.gameObject;
            else return;
            _weaponInstance.transform.localPosition = definition.ViewLocalPosition;
            _weaponInstance.transform.localRotation = Quaternion.Euler(definition.ViewLocalEuler);
            _weaponInstance.transform.localScale = definition.ViewLocalScale;

            var view = _weaponInstance.GetComponent<BattleWeaponView>();
            if (view == null) view = _weaponInstance.AddComponent<BattleWeaponView>();
            _muzzle = view.ResolveMuzzle();
        }

        public bool TryFire()
        {
            if (_currentWeapon == null) return false;
            if (Time.time < _nextFireTime) return false;

            // 지금은 로컬 판정 즉시 발사다.
            // 서버 게임으로 바꿀 때는 여기서 바로 데미지를 확정하지 말고 발사 요청 RPC/패킷을 보내고
            // 서버 승인 결과로 발사체 생성, 히트 판정, 탄약 감소를 확정하는 구조가 안전하다.
            ResolveRig();
            if (_cameraRoot == null) return false;
            if (_muzzle == null) _muzzle = _weaponInstance != null ? _weaponInstance.transform : _weaponMount;
            if (_muzzle == null) return false;

            _nextFireTime = Time.time + _currentWeapon.FireInterval;
            _currentMagazine--;
            NotifyAmmoChanged();

            Vector3 targetPoint = ResolveTargetPoint();
            Vector3 direction = (targetPoint - _muzzle.position).normalized;
            direction = ApplySpread(direction, _currentWeapon.SpreadAngle);

            var projectilePrefab = _currentWeapon.ProjectilePrefab;
            string projectilePoolKey = projectilePrefab != null
                ? $"battle.projectile.{_currentWeapon.WeaponId}"
                : RuntimeProjectilePoolKey;
            GameObject projectileObject = BattlePoolManager.Spawn(
                projectilePoolKey,
                () => CreateProjectileInstance(projectilePrefab),
                _muzzle.position,
                Quaternion.LookRotation(direction));

            var projectile = projectileObject.GetComponent<BattleProjectile>();
            if (projectile == null) projectile = projectileObject.AddComponent<BattleProjectile>();
            projectile.ApplyPresentation(_currentWeapon);
            float effectiveRadius = GetEffectiveProjectileRadius(_currentWeapon);
            projectile.Launch(gameObject, direction, _currentWeapon.MuzzleVelocity, _currentWeapon.Gravity, _currentWeapon.Damage,
                effectiveRadius, _currentWeapon.ProjectileLifetime, _hitMask);
            PlayFireFeedback(direction);
            return true;
        }

        private void ResolveRig()
        {
            if (_cameraRoot == null)
            {
                if (_isLocalControlled && Camera.main != null) _cameraRoot = Camera.main.transform;
                else _cameraRoot = transform;
            }
            if (_weaponMount == null && _autoCreateMount && _cameraRoot != null)
            {
                var mount = _cameraRoot.Find("WeaponMount");
                if (mount == null)
                {
                    var mountGo = new GameObject("WeaponMount");
                    mount = mountGo.transform;
                    mount.SetParent(_cameraRoot, false);
                    mount.localPosition = DefaultWeaponMountLocalPosition;
                    mount.localRotation = Quaternion.identity;
                }
                else if (mount.parent == _cameraRoot && mount.localPosition == Vector3.zero)
                {
                    mount.localPosition = DefaultWeaponMountLocalPosition;
                }
                _weaponMount = mount;
            }
        }

        private void RebuildOwnerPresentation()
        {
            // 오너 여부가 바뀌는 순간 카메라/마운트 기준이 달라진다.
            // 서버에서 소유권이 확정된 뒤 이 경로를 타면 로컬 플레이어만 카메라 하부에 무기 실물이 다시 붙는다.
            _cameraRoot = null;
            _weaponMount = null;
            ResolveRig();
            if (_currentWeapon != null) EquipWeapon(_currentWeapon);
        }

        private void BuildInput()
        {
            _inputMap = JCJInputActions.CreateMap();
            _fireAction = JCJInputActions.Find(_inputMap, JCJInputActions.ActionFire);
            _reloadAction = JCJInputActions.Find(_inputMap, JCJInputActions.ActionReload);
            _aimAction = JCJInputActions.Find(_inputMap, JCJInputActions.ActionAim);
        }

        private void TryReload()
        {
            if (_isReloading || _currentWeapon == null) return;
            if (_reserveAmmo <= 0) return;
            if (_currentMagazine >= _currentWeapon.MagazineSize) return;

            _isReloading = true;
            _reloadFinishTime = Time.time + _currentWeapon.ReloadTime;
            OnReloadStarted?.Invoke(_currentWeapon.ReloadTime);
        }

        private void FinishReload()
        {
            _isReloading = false;
            if (_currentWeapon == null) return;

            int needed = _currentWeapon.MagazineSize - _currentMagazine;
            int transfer = Mathf.Min(needed, _reserveAmmo);
            _currentMagazine += transfer;
            _reserveAmmo -= transfer;
            NotifyAmmoChanged();
        }

        private void NotifyAmmoChanged()
        {
            int magSize = _currentWeapon != null ? _currentWeapon.MagazineSize : 0;
            OnAmmoChanged?.Invoke(_currentMagazine, _reserveAmmo, magSize);
        }

        private Vector3 ResolveTargetPoint()
        {
            if (Physics.Raycast(_cameraRoot.position, _cameraRoot.forward, out var hit, 500f, _hitMask, QueryTriggerInteraction.Ignore))
                return hit.point;
            return _cameraRoot.position + _cameraRoot.forward * 500f;
        }

        private static Vector3 ApplySpread(Vector3 direction, float spreadAngle)
        {
            if (spreadAngle <= 0f) return direction.normalized;

            Quaternion yaw = Quaternion.AngleAxis(Random.Range(-spreadAngle, spreadAngle), Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(Random.Range(-spreadAngle, spreadAngle), Vector3.right);
            return (yaw * pitch * direction).normalized;
        }

        private void PlayFireFeedback(Vector3 direction)
        {
            if (_currentWeapon == null || _muzzle == null) return;

            SpawnShotTracer(direction);

            float recoilStrength = Mathf.Lerp(0.3f, 1.2f,
                Mathf.InverseLerp(5f, 100f, _currentWeapon.Damage));
            if (_currentWeapon.Automatic) recoilStrength *= 0.6f;
            BattleFirstPersonCamera.Shake(recoilStrength);

            if (_currentWeapon.FireSfx != null)
            {
                PlayWeaponSound(_currentWeapon.FireSfx, _muzzle.position, _currentWeapon);
            }
        }

        private void SpawnProceduralMuzzleFlash()
        {
            var flashRoot = new GameObject("BattleMuzzleFlash");
            flashRoot.transform.SetPositionAndRotation(_muzzle.position + _muzzle.forward * 0.04f, _muzzle.rotation);
            float flashScale = GetMuzzleFlashScale(_currentWeapon);
            flashRoot.transform.localScale = Vector3.one * flashScale;

            CreateMuzzleSparks(flashRoot.transform);

            var flashLight = flashRoot.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.8f, 0.4f);
            flashLight.intensity = Mathf.Lerp(1.2f, 3.2f, Mathf.InverseLerp(0.04f, 0.15f, _currentWeapon.ProjectileVisualScale));
            flashLight.range = Mathf.Lerp(1.5f, 3.8f, Mathf.InverseLerp(0.04f, 0.15f, _currentWeapon.ProjectileVisualScale));
            flashLight.shadows = LightShadows.None;
            flashRoot.AddComponent<BattleMuzzleFlashFade>();

            Destroy(flashRoot, 0.15f);
        }

        private void CreateMuzzleSparks(Transform parent)
        {
            var psObj = new GameObject("MuzzleSparks");
            psObj.transform.SetParent(parent, false);

            var ps = psObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.08f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.02f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.5f, 1f),
                new Color(1f, 0.6f, 0.2f, 1f));
            main.gravityModifier = 1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 12;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5, 10) });
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.005f;

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = gradient;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var psr = psObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null) psr.material = CreateSafeParticleMaterial();

            ps.Play();
        }

        private static Material CreateSafeParticleMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("UI/Default");

            var mat = new Material(shader);
            mat.color = new Color(1f, 0.85f, 0.5f, 1f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.renderQueue = 3100;
            return mat;
        }

        private void SpawnShotTracer(Vector3 direction)
        {
            var tracerObject = BattlePoolManager.Spawn(
                TracerPoolKey,
                () =>
                {
                    var instance = new GameObject("BattleShotTracer");
                    instance.AddComponent<BattleTracerEffect>();
                    return instance;
                },
                _muzzle.position,
                Quaternion.identity);
            var tracer = tracerObject.GetComponent<BattleTracerEffect>();
            if (tracer == null) tracer = tracerObject.AddComponent<BattleTracerEffect>();
            float tracerLength = GetTracerLength(_currentWeapon);
            float tracerSpeed = Mathf.Clamp(_currentWeapon.MuzzleVelocity * 2f, 100f, 350f);
            float tracerWidth = GetTracerWidth(_currentWeapon);
            Color tracerColor = _currentWeapon.ProjectileColor;
            tracer.Initialize(
                _muzzle.position,
                direction,
                tracerLength,
                tracerSpeed,
                tracerWidth,
                tracerColor);
        }

        private static GameObject CreateProjectileInstance(GameObject projectilePrefab)
        {
            if (projectilePrefab != null)
            {
                var spawnedProjectile = Instantiate((Object)projectilePrefab);
                if (spawnedProjectile is GameObject projectileGameObject) return projectileGameObject;
                if (spawnedProjectile is Component projectileComponent) return projectileComponent.gameObject;
            }

            var fallback = new GameObject("BattleProjectile");
            fallback.AddComponent<BattleProjectile>();
            return fallback;
        }

        private static void PlayWeaponSound(AudioClip clip, Vector3 position, BattleWeaponDefinition weapon)
        {
            var tempObj = new GameObject("WeaponSfx");
            tempObj.transform.position = position;
            var source = tempObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1f;
            source.volume = 0.95f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.maxDistance = 50f;

            float basePitch = 1f;
            if (weapon.Damage >= 60f) basePitch = Random.Range(0.7f, 0.8f);
            else if (weapon.Automatic && weapon.FireInterval <= 0.1f) basePitch = Random.Range(1.1f, 1.25f);
            else if (weapon.Automatic) basePitch = Random.Range(0.9f, 1.05f);
            else basePitch = Random.Range(0.95f, 1.15f);
            source.pitch = basePitch;

            source.Play();
            Object.Destroy(tempObj, clip.length / basePitch + 0.1f);
        }

        private static Material CreateSafeMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("UI/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Hidden/InternalColored");

            var mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, color);
            if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, color);
            mat.renderQueue = 3100;
            return mat;
        }

        private static float GetEffectiveProjectileRadius(BattleWeaponDefinition definition)
        {
            if (definition == null) return 0.04f;
            float radius = definition.ProjectileRadius;
            if (definition.SpreadAngle >= 1.5f) return Mathf.Clamp(radius * 1.35f, 0.085f, 0.16f);
            if (definition.Damage >= 80f || radius >= 0.1f) return Mathf.Clamp(radius * 1.2f, 0.06f, 0.16f);
            if (definition.Automatic) return Mathf.Clamp(radius * 1.7f, 0.05f, 0.085f);
            return Mathf.Clamp(radius * 1.5f, 0.045f, 0.1f);
        }

        private static float GetMuzzleFlashScale(BattleWeaponDefinition definition)
        {
            if (definition == null) return 1f;
            float scale = definition.ProjectileVisualScale / 0.07f;
            if (definition.Damage >= 80f) scale *= 1.15f;
            return Mathf.Clamp(scale, 0.65f, 1.9f);
        }

        private static float GetTracerLength(BattleWeaponDefinition definition)
        {
            if (definition == null) return 3f;
            float length = definition.MuzzleVelocity * 0.035f + definition.ProjectileTrailTime * 10f + definition.ProjectileVisualScale * 18f;
            return Mathf.Clamp(length, 1.8f, 11f);
        }

        private static float GetTracerWidth(BattleWeaponDefinition definition)
        {
            if (definition == null) return 0.012f;
            float width = Mathf.Max(definition.ProjectileRadius * 0.7f, definition.ProjectileVisualScale * 0.42f);
            return Mathf.Clamp(width, 0.008f, 0.05f);
        }
    }
}
