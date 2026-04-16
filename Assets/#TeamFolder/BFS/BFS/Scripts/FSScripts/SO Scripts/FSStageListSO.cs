using System.Collections.Generic;
using UnityEngine;

namespace BFS
{
    [CreateAssetMenu(fileName = "FSStageListSO", menuName = "BFS_SO/FSStageListSO")]          // List that contains SO, used for each respective stage
    public class FSStageListSO : ScriptableObject
    {
        [field: SerializeField] public FSStageSO[] FSStageList { get; private set; }
    }
}
