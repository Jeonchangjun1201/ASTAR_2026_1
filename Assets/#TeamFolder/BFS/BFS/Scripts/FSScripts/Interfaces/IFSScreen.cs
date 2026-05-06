using UnityEngine;
namespace BFS
{
    public interface IFSScreen                               // Interface for monitor screen in minigame // 모니터 화면을 위한 인터페이스
    {
        void ChangeScreenColor(Color color);                 // Method that gets Color as parameter. Used when changing monitor screen // 색을 매개변수로 받는 메서드, 모니터 화면을 변경하는 데에 사용
        void ResetScreenColor();                             // Method, sets the monitor screen back to its default // 모니터 화면을 검정색으로 지정하는 메서드
    }
}
