using UnityEngine;
namespace BFS
{
    public interface IFSPlate
    {
        PlateColor PlateColor { get; set; }
        void Disappear();
        void Appear();
    }
}
