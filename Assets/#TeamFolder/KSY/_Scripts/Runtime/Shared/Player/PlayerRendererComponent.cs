using KSY.Utility;
using UnityEngine;

namespace KSY.Shared
{
    [RequireComponent(typeof(Animator))]
    public class PlayerRendererComponent : MonoBehaviour
    {
        public Animator AnimatorComponent => _animatorComponent;

        private Animator _animatorComponent;
        private Player _player;

        public void Initialize(Player player)
        {
            if (!gameObject.TryGetComponent(out _animatorComponent))
            {
                CustomLog.LogError("Animator is null");
                return;
            }

            this._player = player;
        }

        public void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0)
        {
            _animatorComponent.CrossFadeInFixedTime(clipHash, crossFadeDuration, layerIndex, normalizedTime);
        }
    }
}
