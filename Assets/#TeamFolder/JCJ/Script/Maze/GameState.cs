namespace _TeamFolder.JCJ.Script
{
    public enum GameState
    {
        Waiting,   // 대기 (미로 생성 후 ~ 게임 시작 전)
        Playing,   // 진행중
        Finished   // 종료 (전원 도착 or 타임오버)
    }
}
