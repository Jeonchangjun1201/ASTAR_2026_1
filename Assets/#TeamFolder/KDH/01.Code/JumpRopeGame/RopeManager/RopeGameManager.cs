using System;
using System.Collections;
using System.Collections.Generic;
using _TeamFolder.KDH._01.Code.SoccerGame.Manager;
using JHJ.Scripts.Test.TestPlayer;
using TMPro;
using UnityEngine;

namespace _TeamFolder.KDH._01.Code.JumpRopeGame.RopeManager
{
    public class RopeGameManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("맵 설정")]
        [SerializeField] private GameObject[] maps; // Element0: 1번맵, Element1: 2번맵
        [SerializeField] private float mapChangeDelay = 2f;

        [Header("카운트다운")]
        [SerializeField] private CountDown countDown;

        [Header("카메라")]
        [SerializeField] private DownTownMapCameraMove cameraSequence;

        [Header("플레이어")]
        [SerializeField] private GameObject[] players;

        public static Action<string> OnPlayerOut;

        private List<string> _alivePlayers = new List<string>
        {
            "Player1", "Player2", "Player3", "Player4"
        };

        private Vector3[] _playerStartPositions;
        private bool _isSwitching = false;
        private int _currentMapIndex = 0;

        private void Awake()
        {
            OnPlayerOut = null;
            OnPlayerOut += HandlePlayerOut;
        }

        private void OnDestroy()
        {
            OnPlayerOut -= HandlePlayerOut;
        }

        private void Start()
        {
            _playerStartPositions = new Vector3[players.Length];
            for (int i = 0; i < players.Length; i++)
                if (players[i] != null)
                    _playerStartPositions[i] = players[i].transform.position;
        }

        private void HandlePlayerOut(string playerName)
        {
            if (_isSwitching) return;
            if (!_alivePlayers.Contains(playerName)) return;

            _alivePlayers.Remove(playerName);
            Debug.Log($"{playerName} Out! 남은 플레이어: {_alivePlayers.Count}");

            if (_alivePlayers.Count <= 1)
            {
                _isSwitching = true;

                string winText = _alivePlayers.Count == 1
                    ? $"{_alivePlayers[0]} Win!"
                    : "jola mothano";

                UpdateText(winText);
                StartCoroutine(SwitchMap());
            }
            else
            {
                UpdateText($"{playerName} Out!\nLeft Player : {_alivePlayers.Count}");
            }
        }

        private IEnumerator SwitchMap()
        {
            yield return new WaitForSeconds(mapChangeDelay);

            UpdateText("");

            // 현재 맵 끄기
            if (maps[_currentMapIndex] != null)
                maps[_currentMapIndex].SetActive(false);

            _currentMapIndex++;

            // 모든 맵 완료
            if (_currentMapIndex >= maps.Length)
            {
                Debug.Log("모든 맵 완료!");
                _currentMapIndex = 0;
                yield break;
            }

            // 다음 맵 켜기
            if (maps[_currentMapIndex] != null)
                maps[_currentMapIndex].SetActive(true);

            // 플레이어 부활
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null)
                {
                    players[i].transform.position = _playerStartPositions[i];
                    players[i].SetActive(true);

                    JHJPlayerController controller =
                        players[i].GetComponent<JHJPlayerController>();
                    if (controller != null)
                        controller.enabled = true;
                }
            }

            _alivePlayers = new List<string>
            {
                "Player1", "Player2", "Player3", "Player4"
            };
            _isSwitching = false;

            if (cameraSequence != null)
                cameraSequence.StartSequence();

            if (countDown != null)
                countDown.RestartCountdown();
        }

        private void UpdateText(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}