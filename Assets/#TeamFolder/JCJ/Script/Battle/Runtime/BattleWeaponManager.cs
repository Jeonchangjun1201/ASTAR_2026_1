using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using _TeamFolder.JCJ.Script;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 무기 장착, 발사, 재장전, 탄약 흐름을 관리하는 매니저.

namespace _TeamFolder.JCJ.Battle
{
    [DefaultExecutionOrder(100)]
    public class BattleWeaponManager : MonoBehaviour
    {
        private static readonly int AimingAnimatorId = Animator.StringToHash("aiming");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
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
        [SerializeField] private Vector3 _weaponMountLocalPosition = new(0.07f, -0.02f, 0.16f);
        [SerializeField] private Vector3 _weaponMountLocalEulerAngles;
        [SerializeField] private Vector3 _weaponMountFineOffset;
        [SerializeField] [FormerlySerializedAs("_liveWeaponMountPoseInInspector")] private bool _suspendWeaponMountAutoSync;
        [SerializeField] private BattleWeaponDefinition _equippedWeaponAssetForInspector;
        [SerializeField] private bool _stripWeaponViewLocalPitchForFirstPerson = true;

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
        private Animator _locomotionAnimator;

        public BattleWeaponDefinition CurrentWeapon => _currentWeapon;
        public bool IsLocallyControlled => _isLocalControlled;
        public int CurrentMagazine => _currentMagazine;
        public int ReserveAmmo => _reserveAmmo;
        public bool IsReloading => _isReloading;

        public event System.Action<int, int, int> OnAmmoChanged;
        public event System.Action<float> OnReloadStarted;
        public event System.Action<BattleShotRequest> ShotRequested;
        public static event System.Action<BattleWeaponManager, BattleShotRequest> AnyShotRequested;

        private void Awake()
        {
            int lb = LayerMask.NameToLayer("BattleLocalBody");
            if (lb >= 0) _hitMask &= ~(1 << lb);
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

        // 로컬 오너 무기의 입력 처리 루프다.
        // 서버 연동 시에도 발사/재장전 요청을 시작하는 진입점으로 보기 좋다.
        private void Update()
        {
            if (!_isLocalControlled) return;
            if (SettingsPanel.IsOpen) return;

            bool aiming = _inputEnabled && _aimAction != null && _aimAction.IsPressed();
            var cam = BattleFirstPersonCamera.Instance;
            if (cam != null) cam.SetAiming(aiming);
            SyncLocomotionAnimatorAiming(aiming);

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

            if (_currentWeapon == null || _fireAction == null) return;

            if (_reloadAction != null && _reloadAction.WasPressedThisFrame()) { TryReload(); return; }

            bool wantsFire = _currentWeapon.Automatic ? _fireAction.IsPressed() : _fireAction.WasPressedThisFrame();
            if (!wantsFire) return;

            if (_currentMagazine <= 0) { TryReload(); return; }

            TryFire();
        }

        private void SyncLocomotionAnimatorAiming(bool value)
        {
            if (_locomotionAnimator == null)
                _locomotionAnimator = GetComponentInChildren<Animator>(true);
            if (_locomotionAnimator == null) return;
            if (_locomotionAnimator.runtimeAnimatorController == null) return;
            _locomotionAnimator.SetBool(AimingAnimatorId, value);
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
                SyncLocomotionAnimatorAiming(false);
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

        // 현재 무기 데이터와 뷰를 교체하는 공통 진입점이다.
        // 서버에서 무기 변경이 확정되면 이 메서드 하나로 탄약, 모델, 총구 기준을 함께 갱신할 수 있다.
        public void EquipWeapon(BattleWeaponDefinition definition)
        {
            _currentWeapon = definition;
            _equippedWeaponAssetForInspector = definition;
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

            if (definition != null)
            {
                ResolveRig();
                ApplyWeaponMountPose(definition);
            }

            if (definition == null || _weaponMount == null) return;
            var viewPrefab = ResolveWeaponViewPrefab(definition);
            if (viewPrefab == null) return;

            var spawnedView = Instantiate((Object)viewPrefab, _weaponMount.position, _weaponMount.rotation, _weaponMount);
            if (spawnedView is GameObject viewGameObject) _weaponInstance = viewGameObject;
            else if (spawnedView is Component viewComponent) _weaponInstance = viewComponent.gameObject;
            else return;
            ApplyWeaponViewTransform(definition);
            RepairWeaponViewMaterials(_weaponInstance);

            var view = _weaponInstance.GetComponent<BattleWeaponView>();
            if (view == null) view = _weaponInstance.AddComponent<BattleWeaponView>();
            _muzzle = view.ResolveMuzzle();
        }

        public void RefreshEquippedWeaponPresentationFromSource()
        {
            if (!_isLocalControlled || _currentWeapon == null) return;
            ResolveRig();
            ApplyWeaponMountPose(_currentWeapon);
            if (_weaponInstance != null) ApplyWeaponViewTransform(_currentWeapon);
        }

#if UNITY_EDITOR
        [ContextMenu("Refresh FP pose from current weapon SO")]
        private void EditorContextRefreshPoseFromSo()
        {
            RefreshEquippedWeaponPresentationFromSource();
        }

        [ContextMenu("Write current weapon presentation to SO (play mode)")]
        private void EditorContextWritePresentationToSo()
        {
            TryEditorWriteCurrentPresentationPositionsToSo();
        }

        private void TryEditorWriteCurrentPresentationPositionsToSo()
        {
            if (!Application.isPlaying) return;
            if (_currentWeapon == null)
            {
                Debug.LogWarning("[BattleWeapon] F: no current weapon SO.", this);
                return;
            }

            if (_weaponMount == null || _weaponInstance == null)
            {
                Debug.LogWarning("[BattleWeapon] F: need WeaponMount and spawned weapon view.", this);
                return;
            }

            ResolveRig();

            EnsurePlayModeWeaponPoseWriteFlushSubscribed();
            Vector3 mount = _weaponMount.localPosition - _weaponMountFineOffset;
            Vector3 mountEuler = _weaponMount.localEulerAngles;
            Vector3 view = _weaponInstance.transform.localPosition;
            string path = AssetDatabase.GetAssetPath(_currentWeapon);
            if (string.IsNullOrEmpty(path))
                Debug.LogWarning("[BattleWeapon] F: SO has no asset path; will not persist after exiting Play Mode.", _currentWeapon);
            else
            {
                if (s_pendingWeaponPoseWritesByAssetPath == null)
                    s_pendingWeaponPoseWritesByAssetPath = new System.Collections.Generic.Dictionary<string, (Vector3, Vector3, Vector3)>();
                s_pendingWeaponPoseWritesByAssetPath[path] = (mount, mountEuler, view);
            }

            _currentWeapon.EditorWritePresentationLocalPositions(mount, mountEuler, view);
            RefreshEquippedWeaponPresentationFromSource();
            Debug.Log(string.IsNullOrEmpty(path)
                ? $"[BattleWeapon] F: applied in Play Mode only (no asset path) '{_currentWeapon.name}'."
                : $"[BattleWeapon] F: '{_currentWeapon.name}' updated in play; disk save runs when Play Mode ends.", _currentWeapon);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            if (!_isLocalControlled || _suspendWeaponMountAutoSync) return;
            ResolveRig();
            if (_weaponMount == null) return;
            SyncWeaponMountToPlayerLocal();
        }

        private static System.Collections.Generic.Dictionary<string, (Vector3 mount, Vector3 mountEuler, Vector3 view)> s_pendingWeaponPoseWritesByAssetPath;
        private static bool s_playModeWeaponPoseWriteFlushSubscribed;

        private static void EnsurePlayModeWeaponPoseWriteFlushSubscribed()
        {
            if (s_playModeWeaponPoseWriteFlushSubscribed) return;
            s_playModeWeaponPoseWriteFlushSubscribed = true;
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChangedFlushWeaponPoseWritesToDisk;
        }

        private static void OnEditorPlayModeStateChangedFlushWeaponPoseWritesToDisk(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            if (s_pendingWeaponPoseWritesByAssetPath == null || s_pendingWeaponPoseWritesByAssetPath.Count == 0) return;
            foreach (var kv in s_pendingWeaponPoseWritesByAssetPath)
            {
                var def = AssetDatabase.LoadAssetAtPath<BattleWeaponDefinition>(kv.Key);
                if (def == null) continue;
                def.EditorWritePresentationLocalPositions(kv.Value.mount, kv.Value.mountEuler, kv.Value.view);
            }

            s_pendingWeaponPoseWritesByAssetPath.Clear();
            AssetDatabase.SaveAssets();
        }

        private static readonly System.Collections.Generic.HashSet<int> s_queuedLiveVisualDefIds = new System.Collections.Generic.HashSet<int>();
        private static bool s_liveVisualFlushScheduled;

        public static void QueueLiveVisualApplyForDefinition(BattleWeaponDefinition definition)
        {
            if (!Application.isPlaying || definition == null) return;
            s_queuedLiveVisualDefIds.Add(definition.GetInstanceID());
            if (s_liveVisualFlushScheduled) return;
            s_liveVisualFlushScheduled = true;
            EditorApplication.delayCall += FlushQueuedLiveVisualDefinitionApplies;
        }

        private static void FlushQueuedLiveVisualDefinitionApplies()
        {
            EditorApplication.delayCall -= FlushQueuedLiveVisualDefinitionApplies;
            s_liveVisualFlushScheduled = false;
            if (!Application.isPlaying)
            {
                s_queuedLiveVisualDefIds.Clear();
                return;
            }

            int[] ids = new int[s_queuedLiveVisualDefIds.Count];
            s_queuedLiveVisualDefIds.CopyTo(ids);
            s_queuedLiveVisualDefIds.Clear();
            var managers = Object.FindObjectsByType<BattleWeaponManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < ids.Length; i++)
            {
                var def = EditorUtility.InstanceIDToObject(ids[i]) as BattleWeaponDefinition;
                if (def == null) continue;
                for (int m = 0; m < managers.Length; m++)
                {
                    var mgr = managers[m];
                    if (mgr == null || !mgr._isLocalControlled) continue;
                    if (mgr._currentWeapon != def) continue;
                    mgr.RefreshEquippedWeaponPresentationFromSource();
                }
            }
        }
#endif

        private void ApplyWeaponViewTransform(BattleWeaponDefinition definition)
        {
            if (_weaponInstance == null || definition == null) return;

            _weaponInstance.transform.localPosition = definition.ViewLocalPosition;
            Vector3 ve = definition.ViewLocalEuler;
            if (_isLocalControlled && _stripWeaponViewLocalPitchForFirstPerson)
                _weaponInstance.transform.localRotation = Quaternion.Euler(0f, ve.y, ve.z);
            else
                _weaponInstance.transform.localRotation = Quaternion.Euler(ve);
            _weaponInstance.transform.localScale = definition.ViewLocalScale;
        }

        private void ApplyWeaponMountPose(BattleWeaponDefinition definition)
        {
            ApplyWeaponMountWorldPose();
        }

        private void ApplyWeaponMountWorldPose()
        {
            ResolveRig();
            if (_weaponMount == null) return;
            if (_weaponMount.parent != transform)
                _weaponMount.SetParent(transform, true);

            Vector3 localPos;
            Quaternion localRot;
            if (_currentWeapon != null && _currentWeapon.UseCustomMountPose)
            {
                localPos = _currentWeapon.MountLocalPosition + _weaponMountFineOffset;
                localRot = Quaternion.Euler(_currentWeapon.MountLocalEulerAngles);
            }
            else
            {
                localPos = _weaponMountLocalPosition + _weaponMountFineOffset;
                localRot = Quaternion.Euler(_weaponMountLocalEulerAngles);
            }

            _weaponMount.localPosition = localPos;
            _weaponMount.localRotation = localRot;
        }

        private static void RepairWeaponViewMaterials(GameObject weaponInstance)
        {
            if (weaponInstance == null) return;
            var renderers = weaponInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                var sharedMaterials = renderer.sharedMaterials;
                bool changed = false;
                for (int j = 0; j < sharedMaterials.Length; j++)
                {
                    var material = sharedMaterials[j];
                    if (material == null) continue;
                    if (material.shader != null && !string.Equals(material.shader.name, "Hidden/InternalErrorShader")) continue;

                    var replacement = new Material(shader);
                    replacement.name = material.name + "_RuntimeFix";
                    if (material.HasProperty(MainTexId) && replacement.HasProperty(MainTexId))
                        replacement.SetTexture(MainTexId, material.GetTexture(MainTexId));
                    var tint = Color.white;
                    if (material.HasProperty(BaseColorId)) tint = material.GetColor(BaseColorId);
                    else if (material.HasProperty(ColorId)) tint = material.GetColor(ColorId);
                    if (replacement.HasProperty(BaseColorId)) replacement.SetColor(BaseColorId, tint);
                    if (replacement.HasProperty(ColorId)) replacement.SetColor(ColorId, tint);
                    sharedMaterials[j] = replacement;
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = sharedMaterials;
            }
        }

        private static GameObject ResolveWeaponViewPrefab(BattleWeaponDefinition definition)
        {
            if (definition == null) return null;
#if UNITY_EDITOR
            var fallbackPath = GetWeaponViewPrefabPath(definition);
            if (!string.IsNullOrEmpty(fallbackPath))
            {
                var editorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fallbackPath);
                if (editorPrefab != null) return editorPrefab;
            }
#endif
            return definition.ViewPrefab;
        }

        private static string GetWeaponViewPrefabPath(BattleWeaponDefinition definition)
        {
            string id = definition.WeaponId != null ? definition.WeaponId.ToLowerInvariant() : string.Empty;
            if (id.Contains("rpg")) return null;
            if (id.Contains("m107")) return "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_AR_01.prefab";
            if (id.Contains("m249") || id.Contains("50cal")) return "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_AR_01.prefab";
            if (id.Contains("bennelli")) return "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_AR_01.prefab";
            if (id.Contains("uzi")) return "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_Handgun_03.prefab";
            if (id.Contains("m1911")) return "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_Handgun_03.prefab";
            if (id.Contains("ak74")) return "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_AR_01.prefab";
            if (id.Contains("m4")) return "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_AR_01.prefab";
            return null;
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
            Vector3 targetPoint = ResolveTargetPoint();
            Vector3 muzzlePos = _muzzle.position;
            Vector3 toTarget = targetPoint - muzzlePos;
            Vector3 aimDir;
            if (toTarget.sqrMagnitude < 1e-8f)
            {
                aimDir = _cameraRoot.forward;
                if (aimDir.sqrMagnitude < 1e-10f) aimDir = transform.forward;
            }
            else
            {
                aimDir = toTarget.normalized;
                Vector3 camFwd = _cameraRoot.forward;
                if (camFwd.sqrMagnitude > 1e-10f && Vector3.Dot(aimDir, camFwd.normalized) < 0.02f)
                    aimDir = camFwd.normalized;
            }

            aimDir.Normalize();
            Vector3 direction = ApplySpread(aimDir, _currentWeapon.SpreadAngle);
            var shotRequest = BuildShotRequest(direction, targetPoint);

            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                ShotRequested?.Invoke(shotRequest);
                AnyShotRequested?.Invoke(this, shotRequest);
                return true;
            }

            ApplyAuthoritativeShot(shotRequest, consumeAmmo: true, playFeedback: true);
            return true;
        }

        public void ApplyAuthoritativeShot(BattleShotRequest shotRequest, bool consumeAmmo, bool playFeedback)
        {
            if (_currentWeapon == null) return;
            if (consumeAmmo)
            {
                _currentMagazine = Mathf.Max(0, _currentMagazine - 1);
                NotifyAmmoChanged();
            }

            var projectilePrefab = _currentWeapon.ProjectilePrefab;
            string projectilePoolKey = projectilePrefab != null
                ? $"battle.projectile.{_currentWeapon.WeaponId}"
                : RuntimeProjectilePoolKey;
            GameObject projectileObject = BattlePoolManager.Spawn(
                projectilePoolKey,
                () => CreateProjectileInstance(projectilePrefab),
                shotRequest.Origin,
                Quaternion.LookRotation(shotRequest.Direction));

            var projectile = projectileObject.GetComponent<BattleProjectile>();
            if (projectile == null) projectile = projectileObject.AddComponent<BattleProjectile>();
            projectile.ApplyPresentation(_currentWeapon);
            projectile.Launch(gameObject, shotRequest.Direction, shotRequest.MuzzleVelocity, shotRequest.Gravity, shotRequest.Damage,
                shotRequest.Radius, shotRequest.Lifetime, _hitMask);
            if (playFeedback) PlayFireFeedback(shotRequest.Direction);
        }

        private BattleShotRequest BuildShotRequest(Vector3 direction, Vector3 targetPoint)
        {
            var identity = RuntimePlayerIdentity.Find(this);
            return new BattleShotRequest
            {
                ShotId = System.Guid.NewGuid().ToString("N"),
                ShooterPlayerId = identity != null ? identity.PlayerId : gameObject.name,
                ShooterDisplayName = identity != null ? identity.DisplayName : gameObject.name,
                WeaponId = _currentWeapon != null ? _currentWeapon.WeaponId : string.Empty,
                Origin = _muzzle != null ? _muzzle.position : transform.position,
                Direction = direction,
                TargetPoint = targetPoint,
                RequestedAt = Time.time,
                MuzzleVelocity = _currentWeapon != null ? _currentWeapon.MuzzleVelocity : 0f,
                Gravity = _currentWeapon != null ? _currentWeapon.Gravity : 0f,
                Damage = _currentWeapon != null ? _currentWeapon.Damage : 0f,
                Radius = _currentWeapon != null ? GetEffectiveProjectileRadius(_currentWeapon) : 0.1f,
                Lifetime = _currentWeapon != null ? _currentWeapon.ProjectileLifetime : 0f
            };
        }

        private void ResolveRig()
        {
            if (_isLocalControlled)
            {
                var mainCam = Camera.main;
                if (mainCam != null && (_cameraRoot == null || ReferenceEquals(_cameraRoot, transform)))
                    _cameraRoot = mainCam.transform;
            }
            if (_cameraRoot == null)
            {
                if (_isLocalControlled && Camera.main != null) _cameraRoot = Camera.main.transform;
                else _cameraRoot = transform;
            }
            if (_weaponMount == null && _autoCreateMount)
            {
                Transform mount = transform.Find("WeaponMount");
                if (mount == null && _cameraRoot != null)
                    mount = _cameraRoot.Find("WeaponMount");
                if (mount == null)
                {
                    var mountGo = new GameObject("WeaponMount");
                    mount = mountGo.transform;
                    mount.SetParent(transform, false);
                }
                else if (mount.parent != transform)
                    mount.SetParent(transform, true);

                _weaponMount = mount;
            }

            if (_weaponMount != null && _weaponMount.parent != transform)
                _weaponMount.SetParent(transform, true);
        }

        private void SyncWeaponMountToPlayerLocal()
        {
            if (!_isLocalControlled) return;
            if (_suspendWeaponMountAutoSync) return;

            ApplyWeaponMountWorldPose();
        }

        private void LateUpdate()
        {
            SyncWeaponMountToPlayerLocal();
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

        // 재장전 완료 시 실제 탄약 수치를 옮기는 지점이다.
        // 서버 구조에서는 완료 시각 검증 뒤 최종 탄 수를 이 메서드와 같은 책임으로 반영하면 된다.
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

        private static Vector3 ApplySpread(Vector3 forward, float spreadAngle)
        {
            if (spreadAngle <= 0f || forward.sqrMagnitude < 1e-10f) return forward.normalized;
            forward.Normalize();
            Quaternion basis = Quaternion.LookRotation(forward);
            float ry = Random.Range(-spreadAngle, spreadAngle);
            float rx = Random.Range(-spreadAngle, spreadAngle);
            return (basis * Quaternion.Euler(rx, ry, 0f) * Vector3.forward).normalized;
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

        private static float GetEffectiveProjectileRadius(BattleWeaponDefinition definition)
        {
            if (definition == null) return 0.04f;
            float radius = definition.ProjectileRadius;
            if (definition.SpreadAngle >= 1.5f) return Mathf.Clamp(radius * 1.35f, 0.085f, 0.16f);
            if (definition.Damage >= 80f || radius >= 0.1f) return Mathf.Clamp(radius * 1.2f, 0.06f, 0.16f);
            if (definition.Automatic) return Mathf.Clamp(radius * 1.7f, 0.05f, 0.085f);
            return Mathf.Clamp(radius * 1.5f, 0.045f, 0.1f);
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
