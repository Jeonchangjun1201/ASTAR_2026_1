using UnityEngine;

namespace BFS
{
    public interface IFSPlate                         // Interface for plates // 발판들을 위한 인터페이스
    {
        PlateColor PlateColor { get; }           // Enum given to each Plates // 각 발판들에게 주어지는 이넘
        void SetPartice(ParticleSystem destroyParticle, ParticleSystem appearParticle);
        void Disappear();                             // Method, used for disabling game object // 게임 오브젝트를 비활성화하는 메서드
        void Appear();                                // Method, used for enabling game object // 게임 오브젝트를 활성화하는 메서드
    }
}
