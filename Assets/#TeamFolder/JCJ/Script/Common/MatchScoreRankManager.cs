using System;
using System.Collections.Generic;
using UnityEngine;
using _TeamFolder.JCJ.Script.Session;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 모드 공용 점수·점수 기준 등수 프리팹. Add/Get/등수 조회만 담당한다.
    /// 미로 골인 순위·포디움·타이머는 <see cref="RankService"/>(GameStateManager 쪽) 전용이다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScoreService))]
    public class MatchScoreRankManager : MonoBehaviour, IMatchScoreRankGateway
    {
        public static MatchScoreRankManager Instance { get; private set; }

        [SerializeField] private bool _persistAcrossScenes = true;

        private ScoreService _score;
        private bool _eventsHooked;

        public IScoreService Score => _score;

        public event Action<string, int, int> OnScoreChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _score = GetComponent<ScoreService>();

            if (_persistAcrossScenes)
                DontDestroyOnLoad(gameObject);

            JcjClientSessionHub.RegisterScoreRank(this);
        }

        private void OnDestroy()
        {
            UnhookEvents();
            if (Instance == this)
            {
                JcjClientSessionHub.UnregisterScoreRank(this);
                Instance = null;
            }
        }

        private void Start() => HookEvents();

        public static MatchScoreRankManager EnsureExists()
        {
            if (Instance != null) return Instance;
            var found = FindFirstObjectByType<MatchScoreRankManager>();
            if (found != null) return found;
            return SceneComponentResolver.FindOrCreate<MatchScoreRankManager>(null, "MatchScoreRankManager");
        }

        public void AddScore(string playerName, int delta) => AddScore(playerName, playerName, delta);

        public void AddScore(string playerId, string displayName, int delta) =>
            _score?.Add(playerId, displayName, delta);

        public void SubtractScore(string playerName, int amount) =>
            SubtractScore(playerName, playerName, amount);

        public void SubtractScore(string playerId, string displayName, int amount) =>
            AddScore(playerId, displayName, -Mathf.Abs(amount));

        public int GetScore(string playerName) => _score != null ? _score.GetScore(playerName) : 0;

        public IReadOnlyList<PlayerRankData> GetScoreRankings() =>
            _score?.GetRankings() ?? Array.Empty<PlayerRankData>();

        public int GetScoreRankForPlayerIndex(int playerIndex) =>
            _score?.GetRankForPlayerIndex(playerIndex) ?? 0;

        public int GetScoreRankByAliases(params string[] aliases) =>
            _score?.GetRankByAliases(aliases) ?? 0;

        public void ResetScores() => _score?.Reset();

        private void HookEvents()
        {
            if (_eventsHooked || _score == null) return;
            _score.OnScoreChanged += HandleScoreChanged;
            _eventsHooked = true;
        }

        private void UnhookEvents()
        {
            if (!_eventsHooked || _score == null) return;
            _score.OnScoreChanged -= HandleScoreChanged;
            _eventsHooked = false;
        }

        private void HandleScoreChanged(string displayName, int delta, int total) =>
            OnScoreChanged?.Invoke(displayName, delta, total);
    }
}
