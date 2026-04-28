namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 일반 타일과 기믹 타일이 공통으로 제공해야 하는 낙하 상태와 낙하 시작 동작.
    /// </summary>
    public interface ITile
    {
        bool HasFallen   { get; }
        bool IsProcessing { get; }
        void StartFalling(bool skipPreDelay = false);
    }
}
