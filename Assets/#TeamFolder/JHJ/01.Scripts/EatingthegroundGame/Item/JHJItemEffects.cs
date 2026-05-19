using UnityEngine;
using JHJ.Scripts.Test.TestPlayer;

namespace JHJ.Scripts.EatingthegroundGame
{
    // 1. 이속 증가
    public class JHJMoveSpeedEffect : JHJIItemEffect
    {
        public void Apply(JHJPlayerController player) => player.SetMoveSpeed(9f);
        public void Remove(JHJPlayerController player) => player.ResetMoveSpeed();
    }

    // 2. 붓 크기 증가
    public class JHJBrushSizeEffect : JHJIItemEffect
    {
        private float _sizeBonus = 40f;
        private float _originalSize;

        public void Apply(JHJPlayerController player)
        {
            var paintManager = Object.FindAnyObjectByType<JHJPaintManager>();
            if (paintManager != null)
            {
                _originalSize = paintManager.brushSize;
                paintManager.brushSize += _sizeBonus;
            }
        }

        public void Remove(JHJPlayerController player)
        {
            var paintManager = Object.FindAnyObjectByType<JHJPaintManager>();
            if (paintManager != null) paintManager.brushSize = _originalSize;
        }
    }

    // 3. 넉백 (밀쳐내기)
    public class JHJKnockbackEffect : JHJIItemEffect
    {
        private Vector3 _itemPos;
        private float _knockbackForce = 12f;

        public JHJKnockbackEffect(Vector3 itemPos) => _itemPos = itemPos;

        public void Apply(JHJPlayerController player)
        {
            if (player.RidCompo != null)
            {
                Vector3 pushDir = (player.transform.position - _itemPos).normalized;
                pushDir.y = 0f;
                if (pushDir == Vector3.zero) pushDir = -player.transform.forward;

                player.RidCompo.AddForce(pushDir * _knockbackForce, ForceMode.Impulse);
            }
        }
        public void Remove(JHJPlayerController player) { /* 넉백은 즉발이라 해제 로직 없음 */ }
    }
}