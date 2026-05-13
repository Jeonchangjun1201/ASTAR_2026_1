using UnityEngine;
namespace BFS
{
    public class RopeTOW : MonoBehaviour, IRopeTOW                             // Rope class //줄 클래스
    {            
        public void MoveRope(Vector3 v)                                        // Method that moves itself to given direction from parameter // 움직이는 메서드
        {
            transform.position += v;
        }
    }

}
