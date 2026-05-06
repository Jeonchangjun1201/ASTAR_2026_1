namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 플레이어 이동, 점프, 착지, 수집 상태를 비주얼 컴포넌트가 애니메이션으로 표현하기 위한 계약.
    /// </summary>
    public interface IPlayerVisual
    {
        void OnIdle();
        void OnWalk(float speedNormalized);
        void OnSprint(float speedNormalized);
        void OnPickup();
        void OnJump();
        void OnFall();
        void OnLand();
        void OnCollect();
        void OnPush();
        void OnThrow();
        void SetCarryState(bool carrying, bool moving);
    }
}
