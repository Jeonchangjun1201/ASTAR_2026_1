using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public class PrimGenerator : IMazeGenerator // Prim: 방사형이고,복잡함
    {
        public int[,] Generate(int w, int h)
        {
            int[,] maze = new int[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                maze[x, y] = 1;

            List<Vector2Int> walls = new List<Vector2Int>();
            Vector2Int start = new Vector2Int(1, 1);
            maze[start.x, start.y] = 0;
            AddWalls(start, walls, w, h);

            while (walls.Count > 0)
            {
                int r = Random.Range(0, walls.Count);
                Vector2Int v = walls[r];
                if (CountPassages(v, maze, w, h) == 1)
                {
                    maze[v.x, v.y] = 0;
                    AddWalls(v, walls, w, h);
                }

                walls.RemoveAt(r);
            }

            return maze;
        }

        private void AddWalls(Vector2Int p, List<Vector2Int> walls, int w, int h)
        {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var d in dirs)
            {
                Vector2Int n = p + d;
                if (n.x > 0 && n.x < w - 1 && n.y > 0 && n.y < h - 1 && !walls.Contains(n)) walls.Add(n);
            }
        }

        private int CountPassages(Vector2Int p, int[,] m, int w, int h)
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