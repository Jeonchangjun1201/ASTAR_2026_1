namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 게임 라운드가 현재 어느 단계에 있는지 나타낸다.
    /// </summary>
    public enum GameState
    {
        Waiting,    // 미로 생성 직후 대기
        Countdown,  // 3-2-1-GO 카운트다운 진행 중
        Playing,    // 진행중
        Finished    // 전원 도착 / 타임오버
    }
}
