using KSY.Shared;
using UnityEngine;

namespace KSY.Servers
{
    public struct CreatePlayerData
    {
        public UnitDataDTO unitData;

        public CreatePlayerData(Player unit)
        {
            Vector2 position = unit.gameObject.transform.position;

            unitData = new UnitDataDTO()
            {
                Position = position
            };
        }
    }
}
