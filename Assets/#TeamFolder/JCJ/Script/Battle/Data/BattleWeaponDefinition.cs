using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 무기 스탯과 프리팹 참조를 담는 정의 에셋.

namespace _TeamFolder.JCJ.Battle
{
    [CreateAssetMenu(menuName = "JCJ/Battle/Weapon Definition", fileName = "WeaponDefinition")]
    public class BattleWeaponDefinition : ScriptableObject
    {
        [SerializeField] private string _weaponId;
        [SerializeField] private string _displayName;
        [SerializeField] private BattleWeaponGrade _grade = BattleWeaponGrade.Rank4;
        [SerializeField] private bool _automatic;
        [SerializeField] private GameObject _viewPrefab;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Vector3 _viewLocalPosition = new(0.18f, -0.18f, 0.35f);
        [SerializeField] private Vector3 _viewLocalEuler = Vector3.zero;
        [SerializeField] private Vector3 _viewLocalScale = Vector3.one;
        [SerializeField] private bool _useCustomMountPose;
        [SerializeField] private Vector3 _mountLocalPosition = new(0.07f, -0.02f, 0.16f);
        [SerializeField] private Vector3 _mountLocalEulerAngles;
        [SerializeField] private float _damage = 20f;
        [SerializeField] private float _fireInterval = 0.18f;
        [SerializeField] private float _muzzleVelocity = 85f;
        [SerializeField] private float _gravity = -18f;
        [SerializeField] private float _projectileLifetime = 4f;
        [SerializeField] private float _projectileRadius = 0.05f;
        [SerializeField] private float _spreadAngle = 0.2f;
        [SerializeField] private GameObject _muzzleFlashPrefab;
        [SerializeField] private GameObject _impactEffectPrefab;
        [SerializeField] private AudioClip _fireSfx;
        [SerializeField] private AudioClip _impactSfx;
        [SerializeField] private Color _projectileColor = new(1f, 0.78f, 0.25f, 1f);
        [SerializeField] private float _projectileVisualScale = 0.08f;
        [SerializeField] private float _projectileTrailTime = 0.18f;

        [Header("Ammo")]
        [SerializeField] private int _magazineSize = -1;
        [SerializeField] private int _totalAmmo = -1;
        [SerializeField] private float _reloadTime = 2f;

        public string WeaponId => _weaponId;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public BattleWeaponGrade Grade => _grade;
        public bool Automatic => _automatic;
        public GameObject ViewPrefab => _viewPrefab;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public Vector3 ViewLocalPosition => _viewLocalPosition;
        public Vector3 ViewLocalEuler => _viewLocalEuler;
        public Vector3 ViewLocalScale => _viewLocalScale;
        public bool UseCustomMountPose => _useCustomMountPose;
        public Vector3 MountLocalPosition => _mountLocalPosition;
        public Vector3 MountLocalEulerAngles => _mountLocalEulerAngles;
        public float Damage => Mathf.Max(0f, _damage);
        public float FireInterval => Mathf.Max(0.01f, _fireInterval);
        public float MuzzleVelocity => Mathf.Max(0.1f, _muzzleVelocity);
        public float Gravity => _gravity;
        public float ProjectileLifetime => Mathf.Max(0.1f, _projectileLifetime);
        public float ProjectileRadius => Mathf.Max(0f, _projectileRadius);
        public float SpreadAngle => Mathf.Max(0f, _spreadAngle);
        public GameObject MuzzleFlashPrefab => _muzzleFlashPrefab;
        public GameObject ImpactEffectPrefab => _impactEffectPrefab;
        public AudioClip FireSfx => _fireSfx;
        public AudioClip ImpactSfx => _impactSfx;
        public Color ProjectileColor => _projectileColor;
        public float ProjectileVisualScale => Mathf.Max(0.01f, _projectileVisualScale);
        public float ProjectileTrailTime => Mathf.Max(0.01f, _projectileTrailTime);

        public int MagazineSize => _magazineSize > 0 ? _magazineSize : DeriveDefaultMagazineSize();
        public int TotalAmmo => _totalAmmo > 0 ? _totalAmmo : DeriveDefaultTotalAmmo();
        public float ReloadTime => Mathf.Max(0.3f, _reloadTime);

        private int DeriveDefaultMagazineSize()
        {
            if (_automatic && _fireInterval <= 0.1f) return 40;
            if (_automatic) return 30;
            if (_damage >= 60f) return 5;
            return 15;
        }

        private int DeriveDefaultTotalAmmo()
        {
            int mag = MagazineSize;
            if (_automatic && _fireInterval <= 0.1f) return mag * 3;
            if (_automatic) return mag * 3;
            if (_damage >= 60f) return mag * 5;
            return mag * 3;
        }

#if UNITY_EDITOR
        public void EditorWritePresentationLocalPositions(Vector3 mountLocalPosition, Vector3 mountLocalEulerAngles, Vector3 viewLocalPosition)
        {
            _mountLocalPosition = mountLocalPosition;
            _mountLocalEulerAngles = mountLocalEulerAngles;
            _viewLocalPosition = viewLocalPosition;
            _useCustomMountPose = true;
            EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            BattleWeaponManager.QueueLiveVisualApplyForDefinition(this);
        }
#endif
    }
}
