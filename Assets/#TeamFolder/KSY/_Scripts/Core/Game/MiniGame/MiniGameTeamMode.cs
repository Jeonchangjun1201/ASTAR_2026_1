namespace KSY.GameCore.MiniGame
{
    public enum MiniGameTeamMode
    {
        None = 0,
        // 1vs1: 두 명이서 서로 대결
        Duel_1v1,

        // 2vs2: 두 명씩 팀을 이뤄 대결
        Tag_2v2,

        // 1vs1vs1vs1: 개인전
        FreeForAll_4P,

        // 1vsAll: 한 명과 나머지의 대결
        OneVsAll,
    }
}

