using PYH.Player;
using Unity.VisualScripting;
using UnityEngine;

namespace KSY.Shared
{
    public class Unit : MonoBehaviour
    {
        public string PlayerID => _playerID;
        private string _playerID = string.Empty;

        private UnitMovementComponent _unitMovementComponent;

        public void Initialize(string playerID)
        {
            this._playerID = playerID;

            GameConfigTable gameConfigTable = GameInstance.DataTableManager.gameConfigTable;
            
            _unitMovementComponent.Initialize();
        }
    }
}


