using UnityEngine;
namespace BFS
{
    public class PlayerTOW : MonoBehaviour                                                   // Player script for Tug of war, might be temporary
    {
        [field: SerializeField] public PlayerInputSOTOW InputSO { get; private set; }
        [field: SerializeField] public bool IsTarget { get; private set; } = false;
    }

}
