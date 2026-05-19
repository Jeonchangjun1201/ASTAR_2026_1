using UnityEngine;

namespace KSY.Shared
{
    [CreateAssetMenu(menuName = "KSY/SO/DataTableManager")]
    public class DataTableManager : ScriptableObject
    {
        public GameConfigTable gameConfigTable = null;

        private void OnEnable()
        {
            gameConfigTable.Initialize();
        }
    }
}

