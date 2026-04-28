using System;
using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 랜덤 벽 후보를 하나씩 열어가며 가지가 적고 자연스러운 미로를 만드는 Prim 방식 생성기.
    /// </summary>
    public class PrimGenerator : IMazeGenerator
    {
        public int[,] Generate(int w, int h, int seed)
        {
            // 전체를 벽으로 채운 뒤 시작점에서 닿을 수 있는 벽 후보만 관리한다.
            var rng = new System.Random(seed);
            int[,] maze = new int[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    maze[x, y] = 1;

            var walls = new List<Vector2Int>();
            var wallSet = new HashSet<Vector2Int>();

            Vector2Int start = new Vector2Int(1, 1);
            maze[start.x, start.y] = 0;
            AddWalls(start, walls, wallSet, w, h);

            while (walls.Count > 0)
            {
                int r = rng.Next(walls.Count);
                Vector2Int v = walls[r];
                int last = walls.Count - 1;
                walls[r] = walls[last];
                walls.RemoveAt(last);
                wallSet.Remove(v);

                if (CountPassages(v, maze) == 1)
                {
                    // 이미 열린 통로가 하나뿐인 벽만 열어 루프가 과하게 생기지 않게 한다.
                    maze[v.x, v.y] = 0;
                    AddWalls(v, walls, wallSet, w, h);
                }
            }

            return maze;
        }

        private static void AddWalls(Vector2Int p, List<Vector2Int> walls, HashSet<Vector2Int> set, int w, int h)
        {
            Span<Vector2Int> dirs = stackalloc Vector2Int[4]
            {
                new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0)
            };
            for (int i = 0; i < 4; i++)
            {
                Vector2Int n = p + dirs[i];
                if (n.x <= 0 || n.x >= w - 1 || n.y <= 0 || n.y >= h - 1) continue;
                if (set.Add(n)) walls.Add(n);
            }
        }

        private static int CountPassages(Vector2Int p, int[,] m)
        {
            int c = 0;
            if (m[p.x + 1, p.y] == 0) c++;
            if (m[p.x - 1, p.y] == 0) c++;
            if (m[p.x, p.y + 1] == 0) c++;
            if (m[p.x, p.y - 1] == 0) c++;
            return c;
        }
    }
}
