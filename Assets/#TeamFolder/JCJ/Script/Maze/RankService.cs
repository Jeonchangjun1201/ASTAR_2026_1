using System;
using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public class RankService : MonoBehaviour, IRankService
    {
        [SerializeField] private ScoreConfig _scoreConfig;
        [SerializeField] private int _totalPlayers = 4;

        public event Action<string, int>          OnPlayerFinished;
        public event Action<List<PlayerRankData>> OnAllFinished;

        private readonly List<PlayerRankData> _rankings = new();
        private int _nextRank = 1;

        public void RegisterFinish(string playerName)
        {
            // 중복 등록 방지
            if (_rankings.Exists(r => r.PlayerName == playerName)) return;

            int rank  = _nextRank++;
            int score = _scoreConfig != null ? _scoreConfig.GetScore(rank) : 0;

            _rankings.Add(new PlayerRankData
            {
                PlayerName = playerName,
                Rank       = rank,
                Score      = score
            });

            OnPlayerFinished?.Invoke(playerName, rank);
            Debug.Log($"[RankService] {rank}등: {playerName} ({score}점)");

            if (_rankings.Count >= _totalPlayers)
                OnAllFinished?.Invoke(_rankings);
        }

        public IReadOnlyList<PlayerRankData> GetRankings() => _rankings;

        // 라운드 재시작 시 초기화
        public void ResetRankings()
        {
            _rankings.Clear();
            _nextRank = 1;
        }
    }
}
