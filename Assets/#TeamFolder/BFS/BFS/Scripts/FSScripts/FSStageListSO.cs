using System.Collections.Generic;
using UnityEngine;

namespace BFS
{
    [CreateAssetMenu(fileName = "FSStageListSO", menuName = "BFS_SO/FSStageListSO")]
    public class FSStageListSO : ScriptableObject
    {
        [field: SerializeField] public FSStageSO[] FSStageList { get; private set; }
    }
}
