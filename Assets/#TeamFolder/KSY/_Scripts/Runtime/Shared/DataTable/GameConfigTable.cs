using KSY.Utility;
using KSY.Shared.DataTable;
using System.Collections.Generic;
using UnityEngine;

namespace KSY.Shared
{
    [System.Serializable]
    public class GameConfigTableRow : KSY_DataTableRow
    {
        public string key = string.Empty;
        public float numberValue = 0f;
        public Object objectValue = null;
    }

    [System.Serializable]
    public class GameConfigTable : DataTable<GameConfigTableRow>
    {
        private Dictionary<string, GameConfigTableRow> tableRowByKey = null;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            tableRowByKey = new Dictionary<string, GameConfigTableRow>();
            foreach (GameConfigTableRow tableRow in this)
                tableRowByKey[tableRow.key] = tableRow;
        }

        private GameConfigTableRow GetRow(string key)
        {
            tableRowByKey.TryGetValue(key, out GameConfigTableRow tableRow);
            return tableRow;
        }

        public Player GetPlayerPrefab()
        {
            GameObject prefab = GetRow("PlayerPrefab").objectValue as GameObject;
            if (prefab != null)
            {
                if (prefab.TryGetComponent(out Player playerCompo))              
                    return playerCompo;       
                else if (prefab.TryGetComponentInChildren<Player>(out var childrenPlayerCompo))
                    return childrenPlayerCompo;
            }

            return null;
        }

        public float GetPlayerSpeed() => GetRow("PlayerSpeed").numberValue;
        public float GetPlayerRotationSpeed() => GetRow("PlayerRotationSpeed").numberValue;
    }
}