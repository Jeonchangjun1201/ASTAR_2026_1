using UnityEngine;

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// TileBoard가 색상과 위치만 넘기면 실제 타일 GameObject를 생성하게 하는 팩토리 계약.
    /// </summary>
    public interface ITileFactory
    {
        BaseTile CreateTile(TileColor color, Vector3 position, Transform parent,
                            float fallDelayMultiplier = 1f);
    }
}
