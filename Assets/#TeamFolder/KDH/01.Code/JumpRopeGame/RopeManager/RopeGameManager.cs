using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

namespace KDH
{
    public class RopeGameManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI statusText;

        public static Action<string> OnPlayerOut;

        private List<string> _alivePlayers = new List<string>
        {
            "Player1", "Player2", "Player3", "Player4"
        };

        private void OnEnable()  => OnPlayerOut += HandlePlayerOut;
        private void OnDisable() => OnPlayerOut -= HandlePlayerOut;

        private void HandlePlayerOut(string playerName)
        {
            _alivePlayers.Remove(playerName);

            if (_alivePlayers.Count > 1)
            {
                // 탈락자 있고 게임 진행 중
                UpdateText($"{playerName} Out!\nLeft Player : {_alivePlayers.Count}");
            }
            else if (_alivePlayers.Count == 1)
            {
                // 최후의 1인 → 우승
                UpdateText($"{_alivePlayers[0]} Win!");
                Debug.Log($"{_alivePlayers[0]} Win!");
            }
            else
            {
                // 전원 탈락
                UpdateText("jola mothano");
            }
        }

        private void UpdateText(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}