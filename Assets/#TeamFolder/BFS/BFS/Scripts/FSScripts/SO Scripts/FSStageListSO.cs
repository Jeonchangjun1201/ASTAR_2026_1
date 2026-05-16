using UnityEngine;

namespace BFS
{
    [CreateAssetMenu(fileName = "FSStageListSO", menuName = "BFS_SO/FSStageListSO")]          // List that contains SO, used for each respective stage // 스테이지 SO를 담는 리스트
    public class FSStageListSO : ScriptableObject
    {
        [field: SerializeField] public FSStageSO[] FSStageList { get; private set; }
        private void OnValidate()
        {
            for (int i = 0; i < FSStageList.Length; i++)
            {
                FSStageList[i].StageIndex = i + 1;
            }
        }
    }
}
