using System;
using UnityEngine;
namespace BFS
{
    public abstract class AbstractTeamTOW : MonoBehaviour, ITeamTOW
    {
        public event Action<ITeamTOW, int> OnScoreGain;
        protected PlayerTOW _player;
        public PlayerTeamTOW Team { get; protected set; }
        public IRopeTOW Rope { get; protected set; }

        public Action<ITeamTOW, Vector2> OnInputPressed { get; set; }

        public string ObjectName { get; protected set; }                              // TEMPORARY; FOR DEBUGGING

        public virtual void Initialize(PlayerTeamTOW team, PlayerTOW player)
        {
            Team = team;
            _player = player;
            _player.InputSO.OnMovementInputPressed += HandleMovekeyPressed;
            ObjectName = GetComponentInParent<PlayerTOW>().gameObject.name;
        }
        protected virtual void OnDestroy()
        {
            _player.InputSO.OnMovementInputPressed -= HandleMovekeyPressed;
        }
        public virtual void ReceiveScore(ITeamTOW team, int score)
        {
            OnScoreGain?.Invoke(team, score);
        }
        private void HandleMovekeyPressed(Vector2 vt)
        {
            if (!_player.IsTarget)
                return;
            OnInputPressed?.Invoke(this, vt);
        }
    }

}
