using KSY.Shared;
using UnityEngine;

namespace KSY.Servers
{
    public struct CreateUnitDataDTO
    {
        public UnitDataDTO unitData;

        public CreateUnitDataDTO(Unit unit)
        {
            Vector2 position = unit.gameObject.transform.position;

            unitData = new UnitDataDTO()
            {
                Position = position
            };
        }
    }
}
