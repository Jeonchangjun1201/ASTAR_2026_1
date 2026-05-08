

// 일반 타일의 기본 동작을 담당하는 구현.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>밟으면 stepDelay 후 낙하하는 기본 타일.</summary>
    public class NormalTile : BaseTile
    {
        public override void OnPlayerStep(PlayerController player)
        {
            StartFalling(); // stepDelay 포함 낙하
        }
    }
}
