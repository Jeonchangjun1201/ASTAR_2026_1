using System.Collections.Generic;

// Sidewinder 방식으로 미로를 생성하는 생성기.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 가로 방향 run을 만들고 위쪽 연결을 하나씩 열어 빠르게 연결 미로를 만드는 Sidewinder 생성기.
    /// </summary>
    public class SidewinderGenerator : IMazeGenerator
    {
        public int[,] Generate(int w, int h, int seed)
        {
            var rng = new System.Random(seed);
            int[,] maze = new int[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    maze[x, y] = 1;

            // 맨 아래 복도 (y = 1)은 전부 열어서 연결성 보장
            for (int x = 1; x < w - 1; x++) maze[x, 1] = 0;

            for (int y = 3; y < h - 1; y += 2)
            {
                var run = new List<int>();
                for (int x = 1; x < w - 1; x += 2)
                {
                    maze[x, y] = 0;
                    run.Add(x);

                    bool atRight = x + 2 >= w - 1;
                    bool closeRun = atRight || rng.NextDouble() > 0.55;

                    if (closeRun)
                    {
                        // run을 닫을 때 그중 하나를 위쪽 줄과 연결해 전체 미로의 연결성을 확보한다.
                        int nx = run[rng.Next(run.Count)];
                        maze[nx, y - 1] = 0;
                        run.Clear();
                    }
                    else
                    {
                        maze[x + 1, y] = 0;
                    }
                }
            }
            return maze;
        }
    }
}
