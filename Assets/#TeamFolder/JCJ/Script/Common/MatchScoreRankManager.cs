using System;
using System.Collections.Generic;
using UnityEngine;
using _TeamFolder.JCJ.Script.Session;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 모드 공용 점수·점수 기준 등수. 미로 골인/포디움/타이머는 <see cref="RankService"/> 전용.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScoreService))]
    public class MatchScoreRankManager : MonoBehaviour, IMatchScoreRankGateway
    {
        /// <summary>씬에 살아 있는 단일 인스턴스. 없으면 null.</summary>
        public static MatchScoreRankManager Instance { get; private set; }

        /// <summary>씬 전환 후에도 유지할지.</summary>
        [SerializeField] private bool _persistAcrossScenes = true;

        private ScoreService _score;
        private bool _eventsHooked;

        /// <summary>내부 점수 저장소. 서버 연동 시 <see cref="IScoreService"/> 이벤트 구독용.</summary>
        public IScoreService Score => _score;

        /// <summary>점수 변경 시 (표시 이름, 변화량, 누적 합계).</summary>
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

        /// <summary>싱글톤 확보. 없으면 씬 탐색 후 프리팹/오브젝트 자동 생성.</summary>
        public static MatchScoreRankManager EnsureExists()
        {
            if (Instance != null) return Instance;
            var found = FindFirstObjectByType<MatchScoreRankManager>();
            if (found != null) return found;
            return SceneComponentResolver.FindOrCreate<MatchScoreRankManager>(null, "MatchScoreRankManager");
        }

        /// <summary>플레이어 이름을 ID로 쓰며 점수 가산. delta는 음수 가능.</summary>
        public void AddScore(string playerName, int delta) => AddScore(playerName, playerName, delta);

        /// <summary>playerId 기준으로 점수 가산·차감. 표시 이름은 HUD용.</summary>
        public void AddScore(string playerId, string displayName, int delta) =>
            _score?.Add(playerId, displayName, delta);

        /// <summary>이름 키로 점수 차감(절댓값만큼 감소).</summary>
        public void SubtractScore(string playerName, int amount) =>
            SubtractScore(playerName, playerName, amount);

        /// <summary>playerId 기준 점수 차감(절댓값만큼 감소).</summary>
        public void SubtractScore(string playerId, string displayName, int amount) =>
            AddScore(playerId, displayName, -Mathf.Abs(amount));

        /// <summary>이름 또는 표시 이름으로 조회한 누적 점수. 없으면 0.</summary>
        public int GetScore(string playerName) => _score != null ? _score.GetScore(playerName) : 0;

        /// <summary>전원 점수 내림차순 순위표. Rank·Score·PlayerId·PlayerName 포함.</summary>
        public IReadOnlyList<PlayerRankData> GetScoreRankings() =>
            _score?.GetRankings() ?? Array.Empty<PlayerRankData>();

        /// <summary>슬롯 인덱스(0부터)에 대응하는 점수 등수. 1=1등, 0=미등록·동점 밖.</summary>
        public int GetScoreRankForPlayerIndex(int playerIndex) =>
            _score?.GetRankForPlayerIndex(playerIndex) ?? 0;

        /// <summary>여러 ID/이름 별칭 중 하나로 점수 등수 조회. 배틀·미로·타일 별칭 매칭.</summary>
        public int GetScoreRankByAliases(params string[] aliases) =>
            _score?.GetRankByAliases(aliases) ?? 0;

        /// <summary>모든 플레이어 점수 테이블 초기화.</summary>
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
