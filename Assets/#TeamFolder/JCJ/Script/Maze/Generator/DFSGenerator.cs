using System.Collections.Generic;
using UnityEngine;

// 깊이 우선 탐색 방식으로 미로를 생성하는 생성기.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 깊이 우선 탐색으로 긴 복도와 막다른 길이 많은 미로를 만드는 생성기.
    /// </summary>
    public class DFSGenerator : IMazeGenerator
    {
        public int[,] Generate(int w, int h, int seed)
        {
            // 두 칸 단위로 이동하며 사이의 벽을 뚫어 벽/통로 격자 구조를 유지한다.
            var rng = new System.Random(seed);
            int[,] maze = new int[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    maze[x, y] = 1;

            var stack = new Stack<Vector2Int>();
            Vector2Int start = new Vector2Int(1, 1);
            maze[start.x, start.y] = 0;
            stack.Push(start);

            var dirs = new[]
            {
                new Vector2Int(0, 2), new Vector2Int(0, -2),
                new Vector2Int(2, 0), new Vector2Int(-2, 0)
            };
            var neighbors = new List<Vector2Int>(4);

            while (stack.Count > 0)
            {
                Vector2Int cur = stack.Peek();
                neighbors.Clear();
                foreach (var d in dirs)
                {
                    Vector2Int n = cur + d;
                    if (n.x > 0 && n.x < w - 1 && n.y > 0 && n.y < h - 1 && maze[n.x, n.y] == 1)
                        neighbors.Add(n);
                }

                if (neighbors.Count > 0)
                {
                    Vector2Int next = neighbors[rng.Next(neighbors.Count)];
                    maze[next.x, next.y] = 0;
                    maze[cur.x + (next.x - cur.x) / 2, cur.y + (next.y - cur.y) / 2] = 0;
                    stack.Push(next);
                }
                else stack.Pop();
            }

            return maze;
        }
    }
}
