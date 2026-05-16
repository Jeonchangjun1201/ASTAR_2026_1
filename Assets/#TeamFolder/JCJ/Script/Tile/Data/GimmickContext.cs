

// 기믹 실행에 필요한 주변 정보를 전달하는 컨텍스트 데이터.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 기믹 초기화 시 공통으로 전달되는 컨텍스트 (Parameter Object 패턴).
    /// 새 설정이 추가돼도 IGimmick 시그니처가 바뀌지 않음 (OCP).
    /// </summary>
    public sealed class GimmickContext
    {
        public GameConfig Config { get; }
        public TileBoard   Board  { get; }

        public GimmickContext(GameConfig config, TileBoard board)
        {
            Config = config;
            Board  = board;
        }
    }
}
