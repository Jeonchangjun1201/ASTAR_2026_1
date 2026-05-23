using KSY.Utility;
using UnityEngine;

namespace KSY.Shared
{
    [RequireComponent(typeof(Animator))]
    public class PlayerRendererComponent : MonoBehaviour
    {
        public Animator AnimatorComponent { get; private set; }

        private Player _player;

        public void Initialize(Player player)
        {
            if (!gameObject.TryGetComponent(out Animator animatorComponent))
            {
                CustomLog.LogError("Animator is null");
                return;
            }

            this._player = player;
        }

        public void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0)
        {
            AnimatorComponent.CrossFadeInFixedTime(clipHash, crossFadeDuration, layerIndex, normalizedTime);
        }
    }
}
