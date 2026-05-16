using UnityEngine;

// 카메라 제어 서비스 계약 인터페이스.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 카메라가 대상 추적, 흔들림, 전체 프레이밍을 제공하기 위한 공통 인터페이스.
    /// </summary>
    public interface ICameraService
    {
        void Follow(Transform target);
        void Shake(float amplitude = 1f, float duration = 0.25f);
        void FrameAll(Vector3 center, Vector3 size);
    }
}
