using System;
using System.Collections.Generic;
using UnityEngine;

//  플레이어별 점수를 저장하고 변경 이벤트를 보내는 서비스.

namespace _TeamFolder.JCJ.Script
{
    public class ScoreService : MonoBehaviour, IScoreService
    {
        [Serializable]
        private struct SeedScoreEntry
        {
            public string playerName;
            public int score;
        }

        private sealed class ScoreEntry
        {
            public string DisplayName;
            public int Score;
        }

        [SerializeField] private bool _persistAcrossScenes = true;
        [SerializeField] private SeedScoreEntry[] _seedScores;

        public static ScoreService Instance { get; private set; }

        public event Action<string, int, int> OnScoreChanged;
        public event Action<string, string, int> ScoreChangeRequested;

        private readonly Dictionary<string, ScoreEntry> _scores = new(StringComparer.OrdinalIgnoreCase);
        private bool _seedApplied;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                MergeInto(Instance);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ApplySeedScoresIfNeeded();

            if (_persistAcrossScenes)
                DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public int GetScore(string playerName)
        {
            ApplySeedScoresIfNeeded();
            if (string.IsNullOrEmpty(playerName)) return 0;
            if (_scores.TryGetValue(playerName, out var entry)) return entry.Score;

            foreach (var pair in _scores)
            {
                if (string.Equals(pair.Value.DisplayName, playerName, StringComparison.OrdinalIgnoreCase))
                    return pair.Value.Score;
            }

            return 0;
        }

        // 점수 누적과 변경 이벤트 발행이 모이는 지점이다.
        // 서버를 붙이면 delta를 로컬에서 계산하기보다 서버가 확정한 점수 변화를 여기로 반영하는 식으로 바꾸기 쉽다.
        public void Add(string playerName, int delta)
        {
            Add(playerName, playerName, delta);
        }

        public void Add(string playerId, string displayName, int delta)
        {
            ApplySeedScoresIfNeeded();
            if (string.IsNullOrWhiteSpace(playerId) || delta == 0) return;

            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                ScoreChangeRequested?.Invoke(playerId, displayName, delta);
                return;
            }

            ApplyScoreDelta(playerId, displayName, delta);
        }

        public void ApplyAuthoritativeDelta(string playerId, string displayName, int delta)
        {
            ApplySeedScoresIfNeeded();
            if (string.IsNullOrWhiteSpace(playerId) || delta == 0) return;
            ApplyScoreDelta(playerId, displayName, delta);
        }

        private void ApplyScoreDelta(string playerId, string displayName, int delta)
        {
            if (string.IsNullOrWhiteSpace(playerId) || delta == 0) return;

            if (!_scores.TryGetValue(playerId, out var entry))
            {
                entry = new ScoreEntry();
                _scores[playerId] = entry;
            }

            if (!string.IsNullOrWhiteSpace(displayName))
                entry.DisplayName = displayName;
            else if (string.IsNullOrWhiteSpace(entry.DisplayName))
                entry.DisplayName = playerId;

            entry.Score += delta;
            OnScoreChanged?.Invoke(entry.DisplayName, delta, entry.Score);
        }

        // 현재 점수 테이블을 정렬해 랭킹 뷰용 목록으로 바꾼다.
        // 서버 응답이 이미 순위 포함이라면 이 메서드는 로컬 폴백이나 표시용 변환기로만 남길 수 있다.
        public IReadOnlyList<PlayerRankData> GetRankings()
        {
            ApplySeedScoresIfNeeded();

            var rankings = new List<PlayerRankData>(_scores.Count);
            foreach (var pair in _scores)
            {
                rankings.Add(new PlayerRankData
                {
                    PlayerId = pair.Key,
                    PlayerName = string.IsNullOrWhiteSpace(pair.Value.DisplayName) ? pair.Key : pair.Value.DisplayName,
                    Score = pair.Value.Score,
                    Rank = 0
                });
            }

            rankings.Sort((a, b) =>
            {
                int scoreCompare = b.Score.CompareTo(a.Score);
                if (scoreCompare != 0) return scoreCompare;
                return string.Compare(a.PlayerName, b.PlayerName, StringComparison.OrdinalIgnoreCase);
            });

            for (int i = 0; i < rankings.Count; i++)
            {
                var entry = rankings[i];
                entry.Rank = i + 1;
                rankings[i] = entry;
            }

            return rankings;
        }

        // 씬 내 플레이어 인덱스를 점수 테이블 이름과 연결하는 보조 규칙이다.
        // 서버 계정 ID나 세션 플레이어 ID를 쓰게 되면 가장 먼저 치환될 가능성이 높은 매핑 지점이다.
        public int GetRankForPlayerIndex(int playerIndex)
        {
            int slot = playerIndex + 1;
            return GetRankByAliases(
                $"maze.player.{slot}",
                $"tile.player.{slot}",
                $"battle.player.{slot}",
                $"Player_{slot}",
                $"Player{slot}",
                $"BattlePlayer_{slot}",
                $"BattlePlayer{slot}");
        }

        public int GetRankByAliases(params string[] aliases)
        {
            if (aliases == null || aliases.Length == 0) return 0;

            var rankings = GetRankings();
            for (int i = 0; i < rankings.Count; i++)
            {
                string rankedId = rankings[i].PlayerId;
                string rankedName = rankings[i].PlayerName;
                for (int j = 0; j < aliases.Length; j++)
                {
                    if (string.IsNullOrEmpty(aliases[j])) continue;
                    if (string.Equals(rankedId, aliases[j], StringComparison.OrdinalIgnoreCase))
                        return rankings[i].Rank;
                    if (string.Equals(rankedName, aliases[j], StringComparison.OrdinalIgnoreCase))
                        return rankings[i].Rank;
                }
            }

            return 0;
        }

        public void Reset()
        {
            _scores.Clear();
            _seedApplied = false;
        }

        private void ApplySeedScoresIfNeeded()
        {
            if (_seedApplied || _scores.Count > 0 || _seedScores == null || _seedScores.Length == 0) return;

            for (int i = 0; i < _seedScores.Length; i++)
            {
                var entry = _seedScores[i];
                if (string.IsNullOrWhiteSpace(entry.playerName)) continue;
                _scores[entry.playerName] = new ScoreEntry
                {
                    DisplayName = entry.playerName,
                    Score = entry.score
                };
            }

            _seedApplied = true;
        }

        private void MergeInto(ScoreService target)
        {
            ApplySeedScoresIfNeeded();
            if (target == null) return;

            foreach (var pair in _scores)
            {
                if (!target._scores.ContainsKey(pair.Key))
                {
                    target._scores[pair.Key] = new ScoreEntry
                    {
                        DisplayName = pair.Value.DisplayName,
                        Score = pair.Value.Score
                    };
                }
            }
        }
    }
}
