using System;
using UnityEngine;
namespace BFS
{
    public abstract class AbstractTeamTOW : MonoBehaviour, ITeamTOW                   // Abstract class for Rope pulling
    {
        public event Action<ITeamTOW, int> OnScoreGain;                               // Action, activates whenever receiving score, ITeamTOW and int as parameter(each are player(Team) and score)
        protected PlayerTOW _player;
        public PlayerTeamTOW Team { get; protected set; }                             // Team enum
        public IRopeTOW Rope { get; protected set; }

        public Action<ITeamTOW, Vector2> OnInputPressed { get; set; }                 // Action, activates whenever input is pressed, ITeamTOW and Vector2 as parameter(each are player and input)

        public string ObjectName { get; protected set; }                              // TEMPORARY; FOR DEBUGGING

        public virtual void Initialize(PlayerTeamTOW team, PlayerTOW player)          // Method for initializing
        {
            Team = team;
            _player = player;
            _player.InputSO.OnMovementInputPressed += HandleMovekeyPressed;
            ObjectName = GetComponentInParent<PlayerTOW>().gameObject.name;
        }
        protected virtual void OnDestroy()                                            // Unsub on destroy
        {
            _player.InputSO.OnMovementInputPressed -= HandleMovekeyPressed;
        }
        public virtual void ReceiveScore(ITeamTOW team, int score)                    // Method to receive score, invokes OnScoreGain action then method(score manager) subbed will give score to team
        {
            OnScoreGain?.Invoke(team, score);
        }
        private void HandleMovekeyPressed(Vector2 vt)                                 // Handles movement key pressed, invokes OnInputPressed action
        {
            if (!_player.IsTarget)                                                    // IsTarget: Only targetted object will receive and invoke inputs
                return;
            OnInputPressed?.Invoke(this, vt);
        }
    }

}
