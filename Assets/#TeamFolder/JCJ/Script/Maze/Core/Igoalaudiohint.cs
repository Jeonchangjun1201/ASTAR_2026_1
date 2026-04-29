namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 골 위치 힌트 사운드를 시작, 중지, 간격 변경할 수 있게 하는 인터페이스.
    /// </summary>
    public interface IGoalAudioHint
    {
        void StartHint();
 
        void StopHint();
 
        
        // 힌트 소리 재생 간격을 런타임에 바꿀 때 호출한다.
        void SetInterval(float interval);
    }
}