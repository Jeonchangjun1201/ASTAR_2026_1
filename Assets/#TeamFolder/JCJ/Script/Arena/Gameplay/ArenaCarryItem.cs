using System.Collections;
using UnityEngine;

namespace _TeamFolder.JCJ.Script.Arena
{
    [RequireComponent(typeof(Rigidbody))]
    public class ArenaCarryItem : MonoBehaviour
    {
        [SerializeField] private string _itemId = "carry_item";
        [SerializeField] private int _requiredStrength = 2;
        [SerializeField] private float _basePickupTime = 0.35f;
        [SerializeField] private float _baseCarryMovePenaltyPercent = 0.10f;
        [SerializeField] private float _baseThrowPower = 12f;
        [SerializeField] private float _holdOffsetY = 1.15f;
        [SerializeField] private float _holdOffsetZ = 0.95f;
        [SerializeField] private float _pickupVisualDuration = 0.12f;
        [SerializeField] private float _releaseOwnerIgnoreSeconds = 0.18f;
        [SerializeField] private float _throwSpinStrength = 12f;

        private Rigidbody _rigidbody;
        private Collider _collider;
        private ArenaPlayerController _owner;
        private Transform _defaultParent;
        private Coroutine _attachRoutine;
        private Coroutine _restoreCollisionRoutine;

        public string ItemId => _itemId;
        public int RequiredStrength => _requiredStrength;
        public float BasePickupTime => _basePickupTime;
        public float BaseCarryMovePenaltyPercent => _baseCarryMovePenaltyPercent;
        public float BaseThrowPower => _baseThrowPower;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _defaultParent = transform.parent;
        }

        public bool CanPickup(int strengthValue)
        {
            return strengthValue >= _requiredStrength - 1;
        }

        public int GetStrengthDelta(int strengthValue)
        {
            return strengthValue - _requiredStrength;
        }

        public float ResolvePickupDurationMultiplier(int strengthValue)
        {
            int delta = GetStrengthDelta(strengthValue);
            if (delta <= -1)
            {
                return ArenaDesignValues.StrengthPenaltyPickupMultiplier;
            }

            if (delta >= 1)
            {
                return ArenaDesignValues.StrengthOverPickupMultiplier;
            }

            return 1f;
        }

        public float ResolveCarryMovePenaltyMultiplier(int strengthValue)
        {
            int delta = GetStrengthDelta(strengthValue);
            if (delta <= -1)
            {
                return ArenaDesignValues.StrengthPenaltyCarryMoveMultiplier;
            }

            if (delta >= 1)
            {
                return 1f;
            }

            return ArenaDesignValues.StrengthNormalCarryMoveMultiplier;
        }

        public float ResolveThrowPowerMultiplier(int strengthValue)
        {
            int delta = GetStrengthDelta(strengthValue);
            if (delta <= -1)
            {
                return ArenaDesignValues.StrengthPenaltyThrowMultiplier;
            }

            if (delta >= 1)
            {
                return ArenaDesignValues.StrengthOverThrowMultiplier;
            }

            return 1f;
        }

        public void AttachToOwner(ArenaPlayerController owner, Transform holdAnchor)
        {
            if (_attachRoutine != null)
            {
                StopCoroutine(_attachRoutine);
                _attachRoutine = null;
            }

            _owner = owner;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            _attachRoutine = StartCoroutine(AnimateAttach(holdAnchor));
        }

        public void Release(Vector3 worldPosition, Quaternion worldRotation, Vector3 throwVelocity)
        {
            if (_attachRoutine != null)
            {
                StopCoroutine(_attachRoutine);
                _attachRoutine = null;
            }

            if (_restoreCollisionRoutine != null)
            {
                StopCoroutine(_restoreCollisionRoutine);
                _restoreCollisionRoutine = null;
            }

            ArenaPlayerController previousOwner = _owner;
            _owner = null;
            transform.SetParent(_defaultParent, true);
            transform.position = worldPosition;
            transform.rotation = worldRotation;

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                Vector3 inheritedVelocity = previousOwner != null ? previousOwner.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero : Vector3.zero;
                _rigidbody.linearVelocity = throwVelocity + inheritedVelocity * 0.45f;
                _rigidbody.angularVelocity = Random.onUnitSphere * _throwSpinStrength;
            }

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            if (previousOwner != null)
            {
                SetIgnoreOwnerCollision(previousOwner, true);
                _restoreCollisionRoutine = StartCoroutine(RestoreOwnerCollision(previousOwner));
            }
        }

        public void Drop(Vector3 worldPosition, Quaternion worldRotation)
        {
            Release(worldPosition, worldRotation, Vector3.zero);
        }

        private IEnumerator AnimateAttach(Transform holdAnchor)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;
            float duration = Mathf.Max(0.02f, _pickupVisualDuration);
            Vector3 holdOffset = new Vector3(0f, _holdOffsetY, _holdOffsetZ);

            transform.SetParent(null, true);
            while (elapsed < duration && holdAnchor != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                Vector3 targetPosition = holdAnchor.TransformPoint(holdOffset);
                transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
                transform.rotation = Quaternion.Slerp(startRotation, holdAnchor.rotation, eased);
                yield return null;
            }

            if (holdAnchor != null)
            {
                transform.SetParent(holdAnchor, false);
                transform.localPosition = holdOffset;
                transform.localRotation = Quaternion.identity;
            }

            _attachRoutine = null;
        }

        private IEnumerator RestoreOwnerCollision(ArenaPlayerController owner)
        {
            yield return new WaitForSeconds(_releaseOwnerIgnoreSeconds);
            SetIgnoreOwnerCollision(owner, false);
            _restoreCollisionRoutine = null;
        }

        private void SetIgnoreOwnerCollision(ArenaPlayerController owner, bool ignore)
        {
            if (owner == null || _collider == null)
            {
                return;
            }

            var ownerColliders = owner.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < ownerColliders.Length; i++)
            {
                if (ownerColliders[i] == null || ownerColliders[i] == _collider)
                {
                    continue;
                }

                Physics.IgnoreCollision(_collider, ownerColliders[i], ignore);
            }
        }
    }
}
