using csiimnida.CSILib.SoundManager.RunTime;
using System;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.Player
{
    public class HumanGolfModule : AbstractMiniGameModule
    {
        [SerializeField] private Rigidbody rigid;
        public event Action<HumanGolfModule, int> OnOutPlayerEvent;
        private bool _isOver;
        
        public void OverPlayer()
        {
            if (_isOver) return;

            _isOver = true;
            OnOutPlayerEvent?.Invoke(this, Index);
        }
        
        public void Push(Vector3 dir, float force)
        {
            SoundManager.Instance.PlaySound("HumanGolf-Hit-S");
            rigid.AddForce(dir * force, ForceMode.Impulse);
        }
    }
}