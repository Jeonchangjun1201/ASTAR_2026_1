using UnityEngine;
namespace GDH
{
    [CreateAssetMenu(fileName = "FSStageSO", menuName = "BFS_SO/FSStageSO")]
    public class FSStageSO : ScriptableObject
    {
        [field: SerializeField] public float ColorDelayTime { get; private set; }
        [field: SerializeField] public int ColorCount { get; private set; }
        [field: SerializeField] public int StageIndex { get; private set; }

        [field: SerializeField] public float PlateDisappearDuration { get; private set; }

        [field: SerializeField] public int CountDownTime { get; private set; }
    }
}
