using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public sealed class RuntimePlayerIdentity : MonoBehaviour
    {
        [SerializeField] private string _playerId;
        [SerializeField] private string _displayName;
        [SerializeField] private int _slotIndex = -1;
        [SerializeField] private bool _isLocalOwned;

        public string PlayerId => _playerId;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? gameObject.name : _displayName;
        public int SlotIndex => _slotIndex;
        public bool IsLocalOwned => _isLocalOwned;

        public void Configure(string playerId, string displayName, int slotIndex, bool isLocalOwned)
        {
            _playerId = string.IsNullOrWhiteSpace(playerId) ? gameObject.name : playerId.Trim();
            _displayName = string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName.Trim();
            _slotIndex = slotIndex;
            _isLocalOwned = isLocalOwned;
        }

        public void SetLocalOwned(bool isLocalOwned)
        {
            _isLocalOwned = isLocalOwned;
        }

        public static RuntimePlayerIdentity Ensure(GameObject target)
        {
            if (target == null) return null;
            var identity = target.GetComponent<RuntimePlayerIdentity>();
            if (identity == null) identity = target.AddComponent<RuntimePlayerIdentity>();
            return identity;
        }

        public static RuntimePlayerIdentity Find(Component source)
        {
            if (source == null) return null;
            return source.GetComponentInParent<RuntimePlayerIdentity>();
        }

        public static bool TryResolve(Component source, out string playerId, out string displayName)
        {
            var identity = Find(source);
            if (identity == null)
            {
                playerId = source != null ? source.gameObject.name : string.Empty;
                displayName = source != null ? source.gameObject.name : string.Empty;
                return false;
            }

            playerId = identity.PlayerId;
            displayName = identity.DisplayName;
            return true;
        }
    }
}
