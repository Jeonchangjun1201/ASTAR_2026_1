using _TeamFolder.PYH._02.Scripts.Util;
using UnityEngine;
using System.Collections.Generic;

namespace JHJ.Scripts.EatingthegroundGame
{
    public class JHJPaintScoreManager : MonoSingleton<JHJPaintScoreManager>
    {
        [Header("계산 설정")]
        [SerializeField] private RenderTexture _paintCanvas;
        [SerializeField] private float _checkInterval = 1f;

        [Header("플레이어 색상 (4개 세팅)")]
        [SerializeField] private Color[] _playerColors = new Color[4];

        // 인스펙터에서 바로 수정 가능한 순위별 보상 점수!
        [Header("순위별 지급 점수 설정 (1등~4등)")]
        [SerializeField] private int[] _rankRewardPoints = new int[4] { 100, 50, 30, 10 };

        private Texture2D _tempTexture;
        [SerializeField] private JHJPaintingGameTimerManager _gameTimer;

        private void Start()
        {
            _tempTexture = new Texture2D(_paintCanvas.width, _paintCanvas.height, TextureFormat.RGBA32, false);
        }

        private void OnEnable()
        {
            if (_gameTimer != null)
                _gameTimer.OnGameEnded += HandleGameEnd;
        }

        private void OnDisable()
        {
            if (_gameTimer != null)
                _gameTimer.OnGameEnded -= HandleGameEnd;
        }

        private void HandleGameEnd()
        {
            PrintGameEndLog();
            CalculateAndPrintRanking();
        }

        private void PrintGameEndLog()
        {
            Debug.Log("======================================");
            Debug.Log(" 게임 종료 ");
            Debug.Log("======================================");
        }

        private void CalculateAndPrintRanking()
        {
            if (_paintCanvas == null || _tempTexture == null) return;

            RenderTexture.active = _paintCanvas;
            _tempTexture.ReadPixels(new Rect(0, 0, _paintCanvas.width, _paintCanvas.height), 0, 0);
            _tempTexture.Apply();
            RenderTexture.active = null;

            Color[] pixels = _tempTexture.GetPixels();
            int totalPixels = pixels.Length;
            int[] playerScores = new int[4];

            for (int i = 0; i < totalPixels; i++)
            {
                Color pColor = pixels[i];
                if (pColor.r > 0.95f && pColor.g > 0.95f && pColor.b > 0.95f) continue;

                for (int p = 0; p < _playerColors.Length; p++)
                {
                    if (Mathf.Abs(pColor.r - _playerColors[p].r) < 1f &&
                        Mathf.Abs(pColor.g - _playerColors[p].g) < 1f &&
                        Mathf.Abs(pColor.b - _playerColors[p].b) < 1f)
                    {
                        playerScores[p]++;
                        break;
                    }
                }
            }

            List<(int playerIndex, float percentage)> rankingList = new List<(int, float)>();
            for (int p = 0; p < _playerColors.Length; p++)
            {
                float percentage = ((float)playerScores[p] / totalPixels) * 100f;
                rankingList.Add((p + 1, percentage));
            }

            // 퍼센트가 높은 순서대로 정렬 (1등부터 4등까지)
            rankingList.Sort((a, b) => b.percentage.CompareTo(a.percentage));

            Debug.Log(" === 땅따먹기 순위 ===");
            for (int i = 0; i < rankingList.Count; i++)
            {
                int playerIndex = rankingList[i].playerIndex;
                float percentage = rankingList[i].percentage;

                //  현재 등수(i)에 맞는 보상 점수 가져오기
                int rewardPoint = 0;
                if (i < _rankRewardPoints.Length)
                {
                    rewardPoint = _rankRewardPoints[i];
                }

                // 디버그로 순위, 퍼센트, 획득한 보상 점수를 한 번에 출력
                Debug.Log($"{i + 1}등: Player {playerIndex} ({percentage:F2}%) -> 획득 보상: {rewardPoint}점!");
            }
        }

        public Color GetPlayerColor(int playerIndex)
        {
            if (playerIndex >= 0 && playerIndex < _playerColors.Length)
                return _playerColors[playerIndex];
            return Color.white;
        }
    }
}