using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public class GameHUD : MonoBehaviour
    {
        [Header("타이머")]
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("순위 피드 (도착 시 1줄씩 추가)")]
        [SerializeField] private TextMeshProUGUI _rankFeedText;

        [Header("결과 패널")]
        [SerializeField] private GameObject      _resultPanel;
        [SerializeField] private TextMeshProUGUI _resultText;

        [Header("대기 패널")]
        [SerializeField] private GameObject _waitingPanel;

        private void Start()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null)
            {
                Debug.LogWarning("[GameHUD] GameStateManager를 찾을 수 없습니다.");
                return;
            }
            gsm.OnStateChanged           += HandleStateChanged;
            gsm.Timer.OnTimerUpdated     += RefreshTimer;
            gsm.Rank.OnPlayerFinished    += AppendRankFeed;
            gsm.Rank.OnAllFinished       += ShowResult;

            _resultPanel?.SetActive(false);
            _waitingPanel?.SetActive(true);
            if (_rankFeedText != null) _rankFeedText.text = string.Empty;
        }

        private void OnDestroy()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null) return;

            gsm.OnStateChanged           -= HandleStateChanged;
            gsm.Timer.OnTimerUpdated     -= RefreshTimer;
            gsm.Rank.OnPlayerFinished    -= AppendRankFeed;
            gsm.Rank.OnAllFinished       -= ShowResult;
        }


        private void HandleStateChanged(GameState state)
        {
            _waitingPanel?.SetActive(state == GameState.Waiting);

            if (state == GameState.Finished)
                _resultPanel?.SetActive(true);
        }

        private void RefreshTimer(float remaining)
        {
            if (_timerText == null) return;

            int min = Mathf.FloorToInt(remaining / 60f);
            int sec = Mathf.FloorToInt(remaining % 60f);
            _timerText.text  = $"{min:00}:{sec:00}";

            // 10초 이하 경고 색
            _timerText.color = remaining <= 10f ? Color.red : Color.white;
        }

        private void AppendRankFeed(string playerName, int rank)
        {
            if (_rankFeedText == null) return;
            string medal = rank switch { 1 => "1", 2 => "2", 3 => "3", _ => $"{rank}등" };
            _rankFeedText.text += $"{medal} {playerName}\n";
        }

        private void ShowResult(List<PlayerRankData> rankings)
        {
            if (_resultText == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("─── 결과 ───");
            foreach (var r in rankings)
                sb.AppendLine($"{r.Rank}등  {r.PlayerName}  {r.Score}점");

            _resultText.text = sb.ToString();
        }
    }
}
