using System;
using System.Collections.Generic;
using UnityEngine;

//  플레이어 순위와 완주 순서를 관리하는 서비스.

namespace _TeamFolder.JCJ.Script
{
    public class RankService : MonoBehaviour, IRankService
    {
        [SerializeField] private ScoreConfig _scoreConfig;
        [SerializeField] private int _totalPlayers = 4;
        [Tooltip("이 인원이 골인하면 게임 종료(포디움 크기).")]
        [SerializeField] private int _podiumSize  = 3;

        public event Action<string, int>          OnPlayerFinished;
        public event Action<PlayerRankData>       OnPlayerFinishedData;
        public event Action<List<PlayerRankData>> OnAllFinished;
        public event Action<string, string>       FinishRequested;

        private readonly List<PlayerRankData> _rankings = new();
        private int _nextRank = 1;
        private bool _allFinishedFired;

        public int PodiumSize => Mathf.Max(1, _podiumSize);
        public int TotalPlayers => Mathf.Max(1, _totalPlayers);

        public void SetTotalPlayers(int total)
        {
            _totalPlayers = Mathf.Max(1, total);
        }

        // 골인 확정 이벤트를 순위 데이터로 바꾸는 지점이다.
        // 서버 구조에서는 클라이언트가 먼저 호출하기보다 서버가 확정한 완주 이벤트를 받아 반영하는 식이 안전하다.
        public void RegisterFinish(string playerName)
        {
            RegisterFinish(playerName, playerName);
        }

        public void RegisterFinish(string playerId, string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerId) && string.IsNullOrWhiteSpace(playerName)) return;
            string resolvedId = string.IsNullOrWhiteSpace(playerId) ? playerName : playerId;
            string resolvedName = string.IsNullOrWhiteSpace(playerName) ? resolvedId : playerName;
            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                FinishRequested?.Invoke(resolvedId, resolvedName);
                return;
            }
            ApplyAuthoritativeFinish(resolvedId, resolvedName);
        }

        public void ApplyAuthoritativeFinish(string playerId, string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerId) && string.IsNullOrWhiteSpace(playerName)) return;
            string resolvedId = string.IsNullOrWhiteSpace(playerId) ? playerName : playerId;
            string resolvedName = string.IsNullOrWhiteSpace(playerName) ? resolvedId : playerName;
            if (_rankings.Exists(r => string.Equals(r.PlayerId, resolvedId, StringComparison.OrdinalIgnoreCase))) return;

            int rank  = _nextRank++;
            int score = _scoreConfig != null ? _scoreConfig.GetScore(rank) : 0;

            var entry = new PlayerRankData
            {
                PlayerId = resolvedId,
                PlayerName = resolvedName,
                Rank       = rank,
                Score      = score
            };
            _rankings.Add(entry);

            OnPlayerFinished?.Invoke(resolvedName, rank);
            OnPlayerFinishedData?.Invoke(entry);
            Debug.Log($"[RankService] #{rank} {resolvedName} ({score} pts)");

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
            // 타이머 종료처럼 추가 골인이 없을 때 현재 순위를 최종 결과로 닫는다.
            // 서버 연동 시에는 매치 종료 패킷을 받은 뒤 이 성격의 확정 처리를 태우면 된다.
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
