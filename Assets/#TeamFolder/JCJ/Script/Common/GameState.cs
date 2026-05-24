
// 라운드 진행 상태를 표현하는 enum (Maze/Tile 공용).

namespace _TeamFolder.JCJ.Script
{
  /// <summary>
  /// 게임 라운드가 현재 어느 단계에 있는지 나타낸다.
  /// </summary>
  public enum GameState
  {
    Waiting,
    Countdown,
    Playing,
    Finished
  }
}
