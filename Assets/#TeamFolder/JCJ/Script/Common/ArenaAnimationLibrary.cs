using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    [CreateAssetMenu(menuName = "JCJ/Common/Animation Library", fileName = "ArenaAnimationLibrary")]
    public class ArenaAnimationLibrary : ScriptableObject
    {
        [SerializeField] private AnimationClip _pickupClip;
        [SerializeField] private AnimationClip _pushClip;
        [SerializeField] private AnimationClip _throwClip;
        [SerializeField] private AnimationClip _carryIdleClip;
        [SerializeField] private AnimationClip _carryMoveClip;

        public AnimationClip PickupClip => _pickupClip;
        public AnimationClip PushClip => _pushClip;
        public AnimationClip ThrowClip => _throwClip;
        public AnimationClip CarryIdleClip => _carryIdleClip;
        public AnimationClip CarryMoveClip => _carryMoveClip;
    }
}
