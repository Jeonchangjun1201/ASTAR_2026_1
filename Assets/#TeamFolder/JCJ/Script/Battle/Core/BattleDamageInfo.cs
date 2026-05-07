using UnityEngine;

namespace _TeamFolder.JCJ.Battle
{
    public struct BattleDamageInfo
    {
        public GameObject Attacker;
        public GameObject Projectile;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public float Damage;
        public bool IsHeadshot;
    }
}
