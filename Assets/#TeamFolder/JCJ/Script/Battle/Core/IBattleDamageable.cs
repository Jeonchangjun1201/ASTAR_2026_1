

// BattleDamageInfo를 받아 피해를 처리하는 대상 계약 인터페이스.

namespace _TeamFolder.JCJ.Battle
{
    public interface IBattleDamageable
    {
        void ApplyDamage(BattleDamageInfo damageInfo);
    }
}
