using System;
using UnityEngine;
namespace BFS
{
    public abstract class AbstractTeamTOW : MonoBehaviour, ITeamTOW                   // Abstract class for Rope pulling // 밧줄을 위한 추상 클래스
    {
        public event Action<ITeamTOW, int> OnScoreGain;                               // Action, activates whenever receiving score, ITeamTOW and int as parameter(each are player(Team) and score) // 점수를 얻을 때 사용되는 액션 함수
        protected PlayerTOW _player;
        public PlayerTeamTOW Team { get; protected set; }                             // Team enum
        public IRopeTOW Rope { get; protected set; }

        public Action<ITeamTOW, Vector2> OnInputPressed { get; set; }                 // Action, activates whenever input is pressed, ITeamTOW and Vector2 as parameter(each are player and input) // 입력을 받을 때 사용되는 액션 함수

        public string ObjectName { get; protected set; }                              // TEMPORARY; FOR DEBUGGING // 임시

        public virtual void Initialize(PlayerTeamTOW team, PlayerTOW player)          // Method for initializing // 이니셜라이즈를 위한 메서드
        {
            Team = team;
            _player = player;
            _player.InputSO.OnMovementInputPressed += HandleMovekeyPressed;
            ObjectName = GetComponentInParent<PlayerTOW>().gameObject.name;
        }
        protected virtual void OnDestroy()                                            // Unsub on destroy // 파괴 시 구독 해제
        {
            _player.InputSO.OnMovementInputPressed -= HandleMovekeyPressed;
        }
        public virtual void ReceiveScore(ITeamTOW team, int score)                    // Method to receive score, invokes OnScoreGain action then method(score manager) subbed will give score to team // 점수 주는 메서드
        {
            OnScoreGain?.Invoke(team, score);
        }
        private void HandleMovekeyPressed(Vector2 vt)                                 // Handles movement key pressed, invokes OnInputPressed action // 눌린 키를 관리함
        {
            if (!_player.IsTarget)                                                    // IsTarget: Only targetted object will receive and invoke inputs // IsTarget을 통해 특정 플레이어만 인풋을 관리함
                return;
            OnInputPressed?.Invoke(this, vt);
        }
    }

}
