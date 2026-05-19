using System;
using UnityEngine;
using System.Collections.Generic;

namespace KDH
{
    public class ScoreManager : MonoBehaviour
    {
        
        private Dictionary<string, int> _scores = new Dictionary<string, int>();

        private void OnEnable()  => Ball.OnGoalScored += HandleGoal;
        private void OnDisable() => Ball.OnGoalScored -= HandleGoal;
        

        private void HandleGoal(GameObject scorer, string goalOwnerName)
        {
            if (scorer == null)
            {
                return;
            }

            string scorerName = scorer.name;

            if (!_scores.ContainsKey(scorerName))
                _scores[scorerName] = 0;

            _scores[scorerName]++;

            Debug.Log($" {scorerName} 득점! ({goalOwnerName} 골대)");
            PrintScores();
        }

        private void PrintScores()
        {
            Debug.Log("=== 현재 점수 ===");
            foreach (var score in _scores)
                Debug.Log($"{score.Key}: {score.Value}점");
        }

        public int GetScore(string playerName)
        {
            return _scores.TryGetValue(playerName, out int score) ? score : 0;
        }
    }
}