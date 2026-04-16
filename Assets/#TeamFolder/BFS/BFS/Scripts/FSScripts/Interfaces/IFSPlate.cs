using UnityEngine;
namespace BFS
{
    public interface IFSPlate                         // Interface for plates
    {
        PlateColor PlateColor { get; }           // Enum given to each Plates
        void Disappear();                             // Method, used for disabling game object
        void Appear();                                // Method, used for enabling game object
    }
}
