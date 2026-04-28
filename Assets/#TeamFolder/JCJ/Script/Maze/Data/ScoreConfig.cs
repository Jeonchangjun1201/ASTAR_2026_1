using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 게임의 등수별 점수와 첫 골인 보너스 규칙을 조정하는 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "ScoreConfig", menuName = "Maze/ScoreConfig")]
    public class ScoreConfig : ScriptableObject
    {
        [Tooltip("인덱스 0 = 1등, 1 = 2등 ...  4인 기준 1·2등만 점수")]
        public int[] ScoresByRank = { 100, 50, 10, 0 };

        [Header("첫 골인 보너스 (남은 인원에 영향)")]
        [Tooltip("첫 번째로 골인한 플레이어가 발생시키는 잔여 시간 단축 비율 (남은시간 * 비율 만큼 차감).")]
        [Range(0f, 1f)]
        public float FirstFinisherTimeShrinkRatio = 0.6f;

        [Tooltip("첫 골인 시 미니맵 전체 공개 여부.")]
        public bool FirstFinisherRevealMap = true;

        public int GetScore(int rank) // rank는 1부터 시작한다.
        {
            int idx = rank - 1;
            return idx >= 0 && idx < ScoresByRank.Length ? ScoresByRank[idx] : 0;
        }
    }
}
