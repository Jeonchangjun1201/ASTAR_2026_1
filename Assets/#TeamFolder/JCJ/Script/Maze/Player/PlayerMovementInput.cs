using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 한 FixedUpdate 틱에 물리 이동 모듈로 넘기는 입력 스냅샷.
    /// 서버 연동 시에는 이 구조체를 네트워크 입력 패킷으로 직렬화하기 쉽다.
    /// </summary>
    public struct PlayerMovementInput
    {
        public Vector2 Move;
        public bool SprintHeld;

        public static PlayerMovementInput Zero => new() { Move = Vector2.zero, SprintHeld = false };
    }
}
