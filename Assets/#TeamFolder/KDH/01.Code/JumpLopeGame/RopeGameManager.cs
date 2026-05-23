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

            if (statusText != null)
                statusText.text = $"{playerName} 탈락!\n남은 플레이어: {_alivePlayers.Count}명";

            if (_alivePlayers.Count == 1)
            {
                if (statusText != null)
                    statusText.text = $"{_alivePlayers[0]} 우승!";
                Debug.Log($"{_alivePlayers[0]} 우승!");
            }
            else if (_alivePlayers.Count == 0)
            {
                if (statusText != null)
                    statusText.text = "전원 탈락!";
            }
        }
    }
}