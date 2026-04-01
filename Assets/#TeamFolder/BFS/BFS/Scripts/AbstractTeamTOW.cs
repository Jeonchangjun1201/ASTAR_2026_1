using UnityEngine;
namespace GDH
{
    public abstract class AbstractTeamTOW : MonoBehaviour, ITeamTOW
    {
        protected int score;
        protected PlayerTOW _player;
        public PlayerTeamTOW Team { get; protected set; }
        public IRopeTOW Rope { get; protected set; }

        public virtual void Initialize(PlayerTeamTOW team, IRopeTOW rope)
        {
            Team = team;
            Rope = rope;
            Rope.InitializeTeam(this);
            _player = GetComponentInParent<PlayerTOW>();

            Debug.Assert(_player != null, "PLAYER IS MISSING! - FAILED TO APPLY PLAYER");

            _player.InputSO.OnMovementInputPressed += HandleMovekeyPressed;
        }
        protected virtual void OnDestroy()
        {
            _player.InputSO.OnMovementInputPressed -= HandleMovekeyPressed;
        }
        public virtual void ReceiveScore(int score)
        {
            this.score += score;
            Debug.Log($"{Team} - {this.score}");
        }
        public virtual void SendInput(IRopeTOW rope, Vector2 input)
        {
            rope.GetInput(input, this);
        }

        private void HandleMovekeyPressed(Vector2 vt) => SendInput(Rope, vt);
    }

}
