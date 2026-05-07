using UnityEngine;

// 캐릭터 연출에 사용할 애니메이션 클립 묶음 에셋.

namespace _TeamFolder.JCJ.Script
{
    [CreateAssetMenu(menuName = "JCJ/Common/Animation Library", fileName = "ArenaAnimationLibrary")]
    public class ArenaAnimationLibrary : ScriptableObject
    {
        [SerializeField] private AnimationClip _pickupClip;
        [SerializeField] private AnimationClip _throwClip;
        [SerializeField] private AnimationClip _carryIdleClip;
        [SerializeField] private AnimationClip _carryMoveClip;

        public AnimationClip PickupClip => _pickupClip;
        public AnimationClip ThrowClip => _throwClip;
        public AnimationClip CarryIdleClip => _carryIdleClip;
        public AnimationClip CarryMoveClip => _carryMoveClip;
    }
}
