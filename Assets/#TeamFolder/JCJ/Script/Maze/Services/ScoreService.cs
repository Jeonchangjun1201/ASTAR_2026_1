using System;
using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 플레이어 이름을 기준으로 점수를 누적하고 변경 결과를 HUD에 알리는 서비스.
    /// </summary>
    public class ScoreService : MonoBehaviour, IScoreService
    {
        public event Action<string, int, int> OnScoreChanged;

        private readonly Dictionary<string, int> _scores = new();

        public int GetScore(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return 0;
            return _scores.TryGetValue(playerName, out var v) ? v : 0;
        }

        public void Add(string playerName, int delta)
        {
            // 같은 플레이어 이름으로 여러 점수 이벤트가 들어오면 누적 합계를 유지한다.
            if (string.IsNullOrEmpty(playerName) || delta == 0) return;
            if (!_scores.TryGetValue(playerName, out var cur)) cur = 0;
            cur += delta;
            _scores[playerName] = cur;
            OnScoreChanged?.Invoke(playerName, delta, cur);
        }

        public void Reset()
        {
            _scores.Clear();
        }
    }
}
