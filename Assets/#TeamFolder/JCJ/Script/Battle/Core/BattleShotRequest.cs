using UnityEngine;

namespace _TeamFolder.JCJ.Battle
{
    public struct BattleShotRequest
    {
        public string ShotId;
        public string ShooterPlayerId;
        public string ShooterDisplayName;
        public string WeaponId;
        public Vector3 Origin;
        public Vector3 Direction;
        public Vector3 TargetPoint;
        public float RequestedAt;
        public float MuzzleVelocity;
        public float Gravity;
        public float Damage;
        public float Radius;
        public float Lifetime;
    }
}
