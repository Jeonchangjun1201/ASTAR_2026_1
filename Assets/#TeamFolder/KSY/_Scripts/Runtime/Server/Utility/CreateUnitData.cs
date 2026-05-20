using KSY.Shared;
using UnityEngine;

namespace KSY.Servers
{
    public struct CreateUnitData
    {
        public UnitDataDTO unitData;

        public CreateUnitData(Unit unit)
        {
            Vector2 position = unit.gameObject.transform.position;

            unitData = new UnitDataDTO()
            {
                Position = position
            };
        }
    }
}
