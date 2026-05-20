using System;
using System.Collections.Generic;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Script.Session
{
    /// <summary>
    /// 매치 전역 점수·점수 기준 등수 API (완주 순위·포디움·타이머 없음).
    /// 구현: <see cref="MatchScoreRankManager"/>. 조회: <see cref="JcjClientSessionHub.TryGetScoreRank"/>.
    /// </summary>
    public interface IMatchScoreRankGateway
    {
        IScoreService Score { get; }

        event Action<string, int, int> OnScoreChanged;

        void AddScore(string playerId, string displayName, int delta);
        void AddScore(string playerName, int delta);
        int GetScore(string playerName);
        IReadOnlyList<PlayerRankData> GetScoreRankings();
        int GetScoreRankForPlayerIndex(int playerIndex);
        int GetScoreRankByAliases(params string[] aliases);
        void ResetScores();
    }
}
