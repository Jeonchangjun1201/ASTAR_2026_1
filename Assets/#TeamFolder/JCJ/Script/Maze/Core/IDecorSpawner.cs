using System.Collections.Generic;
using UnityEngine;

// 장식 오브젝트 배치 서비스 계약 인터페이스.

namespace _TeamFolder.JCJ.Script
{
    public interface IDecorSpawner
    {
        /// <summary>
        /// 미로 내부에 장식 프롭을 흩뿌린다.
        /// data는 0/1 격자이고 excludedCells에는 플레이어 시작점, 골, 코인 위치가 이미 들어 있다.
        /// </summary>
        void Spawn(int[,] data, float cellSize, HashSet<Vector2Int> excludedCells, Transform parent);
    }
}
