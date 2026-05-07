using UnityEngine;

namespace KSY.Shared
{
    public class KSY_DataTableManager : MonoBehaviour
    {
        public KSY_GameConfigTable gameConfigTable = null;

        private void OnEnable()
        {
            gameConfigTable.Initialize();
        }
    }
}

