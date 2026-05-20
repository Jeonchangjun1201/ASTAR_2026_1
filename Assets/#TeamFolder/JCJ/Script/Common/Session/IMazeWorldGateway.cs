using System;
using System.Collections.Generic;
using UnityEngine;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Script.Session
{
    /// <summary>
    /// 미로 월드 생성·격자 데이터·월드 좌표 변환 게이트웨이.
    /// 서버 권한: <see cref="MazeGenerationRequested"/> 구독 후 RPC —
    /// 확정 시 <see cref="ApplyAuthoritativeMaze"/> / <see cref="GenerateMazeData"/>로 동기화.
    /// 구현: MazeManager. 조회: JcjClientSessionHub.TryGetMazeWorld.
    /// </summary>
    public interface IMazeWorldGateway
    {
        int Width { get; }
        int Height { get; }
        float CellSize { get; }
        int[,] MazeData { get; }
        Vector2Int GoalCell { get; }
        IReadOnlyList<GameObject> Players { get; }

        /// <summary>로컬 UI에서 미로 재생성 요청. 서버 모드에서는 RPC 트리거로 사용.</summary>
        event Action<MazeGenerationRequest> MazeGenerationRequested;

        void GenerateMazeWithButton();
        void CreateMaze(AlgorithmType type, int playerCount);
        void ApplyAuthoritativeMaze(AlgorithmType type, int playerCount, int seed);
        int[,] GenerateMazeData(AlgorithmType type, int seed);
        Vector3 CellToWorld(int x, int y);
        bool InBounds(int x, int y);
    }
}
