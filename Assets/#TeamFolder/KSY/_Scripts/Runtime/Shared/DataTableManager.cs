using UnityEngine;

namespace KSY.Shared
{
    public class DataTableManager : MonoBehaviour
    {
        public GameConfigTable gameConfigTable = null;

        private void OnEnable()
        {
            gameConfigTable.Initialize();
        }
    }
}

