using System.Collections.Generic;
using UnityEngine;

// 스태미나 회복 오브를 생성하고 배치하는 스포너.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 통로에 스태미나 오브를 배치하는 스포너 계약.
    /// </summary>
    public interface IMazeStaminaOrbSpawner
    {
        void Spawn(int[,] data, float cellSize, Vector2Int goal,
                   HashSet<Vector2Int> occupied, Transform parent);
    }

    /// <summary>
    /// 미로 곳곳에 스태미나 오브를 배치한다. 막다른 길을 우선해서 탐험 보상처럼 느껴지게 한다.
    /// </summary>
    public class MazeStaminaOrbSpawner : MonoBehaviour, IMazeStaminaOrbSpawner
    {
        [Range(0, 20)] [SerializeField] private int _orbCount = 5;
        [Tooltip("플레이어 시작 공간에서 최소 이 칸 수만큼 떨어뜨린다.")]
        [SerializeField] private int _minCellDistFromStart = 3;
        [SerializeField] private int _minCellDistFromGoal  = 2;

        public void Spawn(int[,] data, float cellSize, Vector2Int goal,
                          HashSet<Vector2Int> occupied, Transform parent)
        {
            if (data == null || parent == null || _orbCount <= 0) return;
            int w = data.GetLength(0);
            int h = data.GetLength(1);

            var deadEnds = new List<Vector2Int>();
            var others   = new List<Vector2Int>();
            for (int x = 1; x < w - 1; x++)
            {
                for (int y = 1; y < h - 1; y++)
                {
                    if (data[x, y] != 0) continue;
                    var c = new Vector2Int(x, y);
                    if (c == goal) continue;

                    int openN = CountOpen(data, x, y);
                    if (openN == 1) deadEnds.Add(c);
                    else            others.Add(c);
                }
            }

            Shuffle(deadEnds);
            Shuffle(others);

            int placed = 0;
            placed += PlaceFrom(deadEnds, occupied, goal, cellSize, parent, _orbCount - placed);
            if (placed < _orbCount)
                placed += PlaceFrom(others, occupied, goal, cellSize, parent, _orbCount - placed);
        }

        private int PlaceFrom(List<Vector2Int> cells, HashSet<Vector2Int> occupied,
                              Vector2Int goal, float cellSize, Transform parent, int wanted)
        {
            int placed = 0;
            foreach (var c in cells)
            {
                if (placed >= wanted) break;
                if (occupied.Contains(c)) continue;
                if (Manhattan(c, goal) < _minCellDistFromGoal) continue;
                if (Manhattan(c, new Vector2Int(1, 1)) < _minCellDistFromStart) continue;

                var go = new GameObject($"StaminaOrb_{c.x}_{c.y}");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(c.x * cellSize, 0f, c.y * cellSize);
                go.AddComponent<StaminaOrb>();

                occupied.Add(c);
                placed++;
            }
            return placed;
        }

        private static int CountOpen(int[,] data, int x, int y)
        {
            int W = data.GetLength(0);
            int H = data.GetLength(1);
            int c = 0;
            if (y + 1 < H && data[x, y + 1] == 0) c++;
            if (y - 1 >= 0 && data[x, y - 1] == 0) c++;
            if (x + 1 < W && data[x + 1, y] == 0) c++;
            if (x - 1 >= 0 && data[x - 1, y] == 0) c++;
            return c;
        }

        private static int Manhattan(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
