using UnityEngine;

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 얼음 기믹 (Cyan 타일).
    /// Configure() 시점에 PhysicsMaterial을 설정해 타일 표면을 미끄럽게 만듦.
    /// 밟히면 NormalTile처럼 stepDelay 후 낙하 (FallsOnActivate = true).
    /// </summary>
    public class IceGimmick : MonoBehaviour, IGimmick
    {
        public bool FallsOnActivate => true;

        public void Configure(GimmickContext ctx)
        {
            ApplyIcePhysicsMaterial();
        }

        private void ApplyIcePhysicsMaterial()
        {
            if (!TryGetComponent<Collider>(out var col)) return;

            // Unity 6 에서는 PhysicsMaterial (s 있음)
            var mat = new PhysicsMaterial("Ice")
            {
                dynamicFriction  = 0.05f,
                staticFriction   = 0.05f,
                frictionCombine  = PhysicsMaterialCombine.Minimum,
                bounciness       = 0f,
                bounceCombine    = PhysicsMaterialCombine.Minimum
            };
            col.material = mat;
        }

        public void OnActivate(BaseTile tile, PlayerController player)
        {
            // FallsOnActivate = true → GimmickTile이 StartFalling() 호출
            // 별도 로직 불필요
        }

        public void OnSubsequentStep(BaseTile tile, PlayerController player) { }
    }
}
