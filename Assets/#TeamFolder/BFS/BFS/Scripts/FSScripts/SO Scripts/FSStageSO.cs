using UnityEngine;
namespace BFS
{
    [CreateAssetMenu(fileName = "FSStageSO", menuName = "BFS_SO/FSStageSO")]
    public class FSStageSO : ScriptableObject                                                      // Scriptable Object used for each stages in Minigame(Obvious, right?) // 각 미니게임들을 위한 SO
    {
        [field: SerializeField] public float ColorDelayTime { get; private set; }
        [field: SerializeField] public int ColorCount { get; private set; }
        [field: SerializeField] public int StageIndex { get; set; }

        [field: SerializeField] public float PlateDisappearDuration { get; private set; }

        [field: SerializeField] public int CountDownTime { get; private set; }
    }
}
