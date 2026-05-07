using System;
using System.Collections.Generic;
using KDH;
using UnityEngine;

namespace KDH
{
    public class SpinManager : MonoBehaviour
    {
        private Dictionary<string, int> _scores = new Dictionary<string, int>();

        private void OnEnable()  => TopManager.OnTopFallen += HandleFallen;
        private void OnDisable() => TopManager.OnTopFallen -= HandleFallen;

        private void HandleFallen(string fallenTop, string lastTouchPlayer)
        {
            if (lastTouchPlayer == "없음")
            {
                Debug.Log($"{fallenTop} 자멸");
                return;
            }

            if (!_scores.ContainsKey(lastTouchPlayer))
                _scores[lastTouchPlayer] = 0;

            _scores[lastTouchPlayer]++;
            Debug.Log($"{lastTouchPlayer} 가 {fallenTop} 을 떨어트림");
            PrintScores();
        }

        private void PrintScores()
        {
            Debug.Log("=== 현재 점수 ===");
            foreach (var kvp in _scores)
                Debug.Log($"{kvp.Key}: {kvp.Value}점");
        }
    }
}
    

