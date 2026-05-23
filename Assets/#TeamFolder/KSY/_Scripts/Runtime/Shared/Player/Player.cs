using Players;
using UnityEngine;

namespace KSY.Shared
{
    public class Player : MonoBehaviour
    {
        [SerializeField]
        private PlayerMovementComponent movementComponent = null;
        public PlayerMovementComponent MovementComponent => movementComponent;

        public string PlayerID => _playerID;
        private string _playerID = string.Empty;

        public void Initialize(string playerID)
        {
            this._playerID = playerID;

            GameConfigTable gameConfigTable = GameInstance.DataTableManager.gameConfigTable;
            
            movementComponent.Initialize(gameConfigTable.GetPlayerMaxSpeed());
        }
    }
}


