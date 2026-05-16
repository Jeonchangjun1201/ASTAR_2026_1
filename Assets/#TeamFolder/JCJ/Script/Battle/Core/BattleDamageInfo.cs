using UnityEngine;

// 공격자, 피해량, 판정 정보를 함께 전달하는 피해 데이터.

namespace _TeamFolder.JCJ.Battle
{
    public struct BattleDamageInfo
    {
        public string AttackerId;
        public string AttackerDisplayName;
        public string TargetId;
        public string TargetDisplayName;
        public string WeaponId;
        public GameObject Attacker;
        public GameObject Target;
        public GameObject Projectile;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public float Damage;
        public bool IsHeadshot;
    }
}
