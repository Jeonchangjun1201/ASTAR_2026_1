using System;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.Player
{
    public class PassTheBombModule : AbstractMiniGameModule
    {
        public Action<PassTheBombModule, int> onExplosionEvent;
        public event Action<PassTheBombModule> OnTouchPlayerEvent;
        
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.TryGetComponent(out PassTheBombModule player)) return;
                
            OnTouchPlayerEvent?.Invoke(player);
        }
    }
}