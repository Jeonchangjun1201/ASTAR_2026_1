using UnityEngine;
namespace BFS
{
    public class PlayerTOW : MonoBehaviour                                                   // Player script for Tug of war, might be temporary // 줄다리기 전용 플레이어 스크립트, 임시일 수도 있음
    {
        [field: SerializeField] public PlayerInputSOTOW InputSO { get; private set; }
        [field: SerializeField] public bool IsTarget { get; private set; } = false;
    }

}
