using System;
using System.Collections.Generic;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Script.Session
{
    /// <summary>
    /// 매치 전역 점수·점수 기준 등수 API. 구현: <see cref="MatchScoreRankManager"/>.
    /// </summary>
    public interface IMatchScoreRankGateway
    {
        /// <summary>내부 <see cref="ScoreService"/> (서버 권한 시 ScoreChangeRequested 등).</summary>
        IScoreService Score { get; }

        /// <summary>점수 변경 알림 (표시 이름, delta, 합계).</summary>
        event Action<string, int, int> OnScoreChanged;

        /// <summary>playerId 기준 점수 가산·차감.</summary>
        void AddScore(string playerId, string displayName, int delta);

        /// <summary>이름을 ID로 쓰는 점수 가산·차감.</summary>
        void AddScore(string playerName, int delta);

        /// <summary>누적 점수 조회.</summary>
        int GetScore(string playerName);

        /// <summary>점수 기준 전체 순위표.</summary>
        IReadOnlyList<PlayerRankData> GetScoreRankings();

        /// <summary>플레이어 슬롯 인덱스 → 점수 등수(1~N).</summary>
        int GetScoreRankForPlayerIndex(int playerIndex);

        /// <summary>별칭 목록으로 점수 등수 조회.</summary>
        int GetScoreRankByAliases(params string[] aliases);

        /// <summary>점수 테이블 초기화.</summary>
        void ResetScores();
    }
}
