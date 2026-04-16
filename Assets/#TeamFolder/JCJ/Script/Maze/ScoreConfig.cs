using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    [CreateAssetMenu(fileName = "ScoreConfig", menuName = "Maze/ScoreConfig")]
    public class ScoreConfig : ScriptableObject
    {
        [Tooltip("인덱스 0 = 1등, 1 = 2등 ...  4인 기준 1·2등만 점수")]
        public int[] ScoresByRank = { 100, 50, 10, 0 };

        public int GetScore(int rank) // rank: 1-based
        {
            int idx = rank - 1;
            return idx >= 0 && idx < ScoresByRank.Length ? ScoresByRank[idx] : 0;
        }
    }
}
