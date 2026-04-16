using UnityEngine;
namespace BFS
{
    public class PlayerTOW : MonoBehaviour
    {
        [field: SerializeField] public PlayerInputSOTOW InputSO { get; private set; }
        [field: SerializeField] public bool IsTarget { get; private set; } = false;
    }

}
