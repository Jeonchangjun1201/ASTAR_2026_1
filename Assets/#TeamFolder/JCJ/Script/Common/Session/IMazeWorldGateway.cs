using System;
using System.Collections.Generic;
using UnityEngine;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Script.Session
{
    public interface IMazeWorldGateway
    {
        int Width { get; }
        int Height { get; }
        float CellSize { get; }
        int[,] MazeData { get; }
        Vector2Int GoalCell { get; }
        IReadOnlyList<GameObject> Players { get; }
        event Action<MazeGenerationRequest> MazeGenerationRequested;
        void GenerateMazeWithButton();
        void CreateMaze(AlgorithmType type, int playerCount);
        void ApplyAuthoritativeMaze(AlgorithmType type, int playerCount, int seed);
        int[,] GenerateMazeData(AlgorithmType type, int seed);
        Vector3 CellToWorld(int x, int y);
        bool InBounds(int x, int y);
    }
}
