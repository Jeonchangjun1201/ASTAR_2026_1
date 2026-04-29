using System;
using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public class RankService : MonoBehaviour, IRankService
    {
        [SerializeField] private ScoreConfig _scoreConfig;
        [SerializeField] private int _totalPlayers = 4;
        [Tooltip("이 인원이 골인하면 게임 종료(포디움 크기).")]
        [SerializeField] private int _podiumSize  = 3;

        public event Action<string, int>          OnPlayerFinished;
        public event Action<List<PlayerRankData>> OnAllFinished;

        private readonly List<PlayerRankData> _rankings = new();
        private int _nextRank = 1;
        private bool _allFinishedFired;

        public int PodiumSize => Mathf.Max(1, _podiumSize);
        public int TotalPlayers => Mathf.Max(1, _totalPlayers);

        public void SetTotalPlayers(int total)
        {
            _totalPlayers = Mathf.Max(1, total);
        }

        public void RegisterFinish(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return;
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
            Debug.Log($"[RankService] #{rank} {playerName} ({score} pts)");

            if (_allFinishedFired) return;
            int threshold = Mathf.Min(PodiumSize, TotalPlayers);
            if (_rankings.Count >= threshold)
            {
                _allFinishedFired = true;
                OnAllFinished?.Invoke(_rankings);
            }
        }

        /// <summary>지금 즉시 종료 이벤트 확정(예: 타이머 만료).</summary>
        public void FinalizeNow()
        {
            if (_allFinishedFired) return;
            _allFinishedFired = true;
            OnAllFinished?.Invoke(_rankings);
        }

        public IReadOnlyList<PlayerRankData> GetRankings() => _rankings;

        public void ResetRankings()
        {
            _rankings.Clear();
            _nextRank = 1;
            _allFinishedFired = false;
        }
    }
}
