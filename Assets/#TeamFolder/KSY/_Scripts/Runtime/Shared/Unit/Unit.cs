using KSY.Clients;
using UnityEngine;

namespace KSY.Shared
{
    public class Unit : MonoBehaviour
    {
        [SerializeField]
        private UnitMovementComponent unitMovementComponent = null;
        public UnitMovementComponent UnitMovementComponent => unitMovementComponent;

        public string PlayerID => _playerID;
        private string _playerID = string.Empty;


        public void Initialize(string playerID)
        {
            this._playerID = playerID;

            GameConfigTable gameConfigTable = GameInstance.DataTableManager.gameConfigTable;
            
            unitMovementComponent.Initialize(gameConfigTable.GetUnitMaxSpeed(), gameConfigTable.GetUnitAcceleration());
        }
    }
}


