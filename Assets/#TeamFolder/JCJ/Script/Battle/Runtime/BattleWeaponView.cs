using UnityEngine;

// 손에 들린 무기 모델과 발사 위치 기준을 구성하는 뷰.

namespace _TeamFolder.JCJ.Battle
{
    public class BattleWeaponView : MonoBehaviour
    {
        [SerializeField] private Transform _muzzle;

        public Transform ResolveMuzzle()
        {
            if (_muzzle != null) return _muzzle;

            var named = FindNamedMuzzle(transform);
            if (named != null)
            {
                _muzzle = named;
                return _muzzle;
            }

            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                _muzzle = transform;
                return _muzzle;
            }

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var muzzleObject = new GameObject("Muzzle");
            var muzzleTransform = muzzleObject.transform;
            muzzleTransform.SetParent(transform, false);
            var localPosition = transform.InverseTransformPoint(bounds.center + transform.forward * bounds.extents.z);
            muzzleTransform.localPosition = localPosition;
            muzzleTransform.localRotation = Quaternion.identity;
            _muzzle = muzzleTransform;
            return _muzzle;
        }

        private static Transform FindNamedMuzzle(Transform root)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var lowered = child.name.ToLowerInvariant();
                if (lowered.Contains("muzzle") || lowered.Contains("barrel") || lowered.Contains("flash"))
                    return child;
                var nested = FindNamedMuzzle(child);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
