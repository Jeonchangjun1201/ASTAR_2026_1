namespace _TeamFolder.JCJ.Script
{
    public interface IGoalAudioHint
    {
        void StartHint();
 
        void StopHint();
 
        
        void SetInterval(float interval);// 간격 변경 시 호출ㄱㄱ
    }
}