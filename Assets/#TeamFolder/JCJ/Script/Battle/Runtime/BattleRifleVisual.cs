using System.Collections;
using UnityEngine;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Battle
{
    public class BattleRifleVisual : MonoBehaviour, IPlayerVisual
    {
        private const string WeaponMountName = "BattleRifleWeaponMount";
        private const float DeathFadeDuration = 1.2f;

        private PartyCharacterVisual _delegateVisual;
        private Transform _cachedAttachPoint;
        private bool _isDead;
        private Coroutine _deathRoutine;

        private void Awake()
        {
            _delegateVisual = GetComponent<PartyCharacterVisual>();
        }

        private PartyCharacterVisual ResolveDelegate()
        {
            if (_delegateVisual == null) _delegateVisual = GetComponent<PartyCharacterVisual>();
            return _delegateVisual;
        }

        public void OnIdle()
        {
            var del = ResolveDelegate();
            if (del != null) del.OnIdle();
        }

        public void OnWalk(float speedNormalized)
        {
            var del = ResolveDelegate();
            if (del != null) del.OnWalk(speedNormalized);
        }

        public void OnSprint(float speedNormalized)
        {
            var del = ResolveDelegate();
            if (del != null) del.OnSprint(speedNormalized);
        }

        public void OnPickup()
        {
            var del = ResolveDelegate();
            if (del != null) del.OnPickup();
        }

        public void OnJump()
        {
            var del = ResolveDelegate();
            if (del != null) del.OnJump();
        }

        public void OnFall()
        {
            var del = ResolveDelegate();
            if (del != null) del.OnFall();
        }

        public void OnLand()
        {
            var del = ResolveDelegate();
            if (del != null) del.OnLand();
        }

        public void OnCollect()
        {
            var del = ResolveDelegate();
            if (del != null) del.OnCollect();
        }

        public void OnThrow()
        {
            var del = ResolveDelegate();
            if (del != null) del.OnThrow();
        }

        public void SetCarryState(bool carrying, bool moving)
        {
            var del = ResolveDelegate();
            if (del != null) del.SetCarryState(carrying, moving);
        }

        public Transform ResolveWeaponAttachPoint()
        {
            if (_cachedAttachPoint != null) return _cachedAttachPoint;

            var animator = GetComponentInChildren<Animator>(true);
            if (animator != null && animator.isHuman)
            {
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand != null)
                {
                    _cachedAttachPoint = hand;
                    return _cachedAttachPoint;
                }
            }

            var existing = transform.Find(WeaponMountName);
            if (existing != null)
            {
                _cachedAttachPoint = existing;
                return _cachedAttachPoint;
            }

            var mount = new GameObject(WeaponMountName).transform;
            mount.SetParent(transform, false);
            mount.localPosition = new Vector3(0.18f, 1.05f, 0.15f);
            mount.localRotation = Quaternion.identity;
            _cachedAttachPoint = mount;
            return _cachedAttachPoint;
        }

        public void PlayDeath()
        {
            if (_isDead) return;
            _isDead = true;

            var animator = GetComponentInChildren<Animator>(true);
            if (animator != null) animator.enabled = false;

            if (_deathRoutine != null) StopCoroutine(_deathRoutine);
            if (isActiveAndEnabled) _deathRoutine = StartCoroutine(DeathFadeRoutine());
        }

        public void ResetDeathState()
        {
            _isDead = false;
            if (_deathRoutine != null)
            {
                StopCoroutine(_deathRoutine);
                _deathRoutine = null;
            }

            var animator = GetComponentInChildren<Animator>(true);
            if (animator != null) animator.enabled = true;

            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
                if (renderer != null) renderer.enabled = true;
        }

        private IEnumerator DeathFadeRoutine()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            float elapsed = 0f;
            while (elapsed < DeathFadeDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            foreach (var renderer in renderers)
                if (renderer != null) renderer.enabled = false;

            _deathRoutine = null;
        }
    }
}
