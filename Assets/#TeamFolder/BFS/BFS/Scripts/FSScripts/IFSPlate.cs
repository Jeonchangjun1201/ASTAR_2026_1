using UnityEngine;
namespace GDH
{
    public interface IFSPlate
    {
        PlateColor PlateColor { get; set; }
        void Disappear();
        void Appear();
    }
}
