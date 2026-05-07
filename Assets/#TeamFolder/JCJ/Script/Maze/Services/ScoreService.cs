using System;
using System.Collections.Generic;
using UnityEngine;

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

        [SerializeField] private bool _persistAcrossScenes = true;
        [SerializeField] private SeedScoreEntry[] _seedScores;

        public static ScoreService Instance { get; private set; }

        public event Action<string, int, int> OnScoreChanged;

        private readonly Dictionary<string, int> _scores = new(StringComparer.OrdinalIgnoreCase);
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
            return _scores.TryGetValue(playerName, out var v) ? v : 0;
        }

        public void Add(string playerName, int delta)
        {
            ApplySeedScoresIfNeeded();
            if (string.IsNullOrEmpty(playerName) || delta == 0) return;
            if (!_scores.TryGetValue(playerName, out var cur)) cur = 0;
            cur += delta;
            _scores[playerName] = cur;
            OnScoreChanged?.Invoke(playerName, delta, cur);
        }

        public IReadOnlyList<PlayerRankData> GetRankings()
        {
            ApplySeedScoresIfNeeded();

            var rankings = new List<PlayerRankData>(_scores.Count);
            foreach (var pair in _scores)
            {
                rankings.Add(new PlayerRankData
                {
                    PlayerName = pair.Key,
                    Score = pair.Value,
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

        public int GetRankForPlayerIndex(int playerIndex)
        {
            int slot = playerIndex + 1;
            return GetRankByAliases(
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
                string rankedName = rankings[i].PlayerName;
                for (int j = 0; j < aliases.Length; j++)
                {
                    if (string.IsNullOrEmpty(aliases[j])) continue;
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
                _scores[entry.playerName] = entry.score;
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
                    target._scores[pair.Key] = pair.Value;
            }
        }
    }
}
