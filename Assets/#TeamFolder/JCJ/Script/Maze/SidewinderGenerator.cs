using System.Collections.Generic;
using UnityEngine;
namespace _TeamFolder.JCJ.Script
{
    public class SidewinderGenerator : IMazeGenerator //Sidewinder: 한쪽 방향으로 흐르는 미로
    {
        public int[,] Generate(int w, int h)
        {
            int[,] maze = new int[w, h];
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) maze[x, y] = 1;//모든 노드 노드를 1로 초기화

            for (int y = 1; y < h - 1; y += 2)
            {
                List<int> run = new List<int>();
                for (int x = 1; x < w - 1; x += 2)
                {
                    maze[x, y] = 0;
                    run.Add(x);
                    if (y > 1 && (x + 2 >= w - 1 || Random.value > 0.5f))
                    {
                        int nx = run[Random.Range(0, run.Count)];
                        maze[nx, y - 1] = 0;
                        run.Clear();
                    }
                    else if (x + 2 < w - 1) maze[x + 1, y] = 0;
                }
            }
            return maze;
        }
    }}