namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 기믹 타일. IGimmick을 TileFactory에서 주입받아 동작.
    /// FallsOnActivate 에 따라 낙하 시점이 결정됨.
    /// </summary>
    public class GimmickTile : BaseTile
    {
        private IGimmick _gimmick;
        private bool     _gimmickActivated;

        /// <summary>TileFactory가 생성 직후 호출.</summary>
        public void SetGimmick(IGimmick gimmick) => _gimmick = gimmick;

        public override void OnPlayerStep(PlayerController player)
        {
            // 기믹 없으면 NormalTile처럼 동작
            if (_gimmick == null) { StartFalling(); return; }

            if (!_gimmickActivated)
            {
                _gimmickActivated = true;
                _gimmick.OnActivate(this, player);

                if (_gimmick.FallsOnActivate)
                    StartFalling(); // stepDelay 포함
            }
            else if (!_gimmick.FallsOnActivate)
            {
                _gimmick.OnSubsequentStep(this, player);
            }
        }
    }
}
