using System.Collections.Generic;
using UnityEngine;
namespace BFS
{
    public class RopeTOW : MonoBehaviour, IRopeTOW                             // Rope class
    {            
        public void MoveRope(Vector3 v)                                        // Method that moves itself to given direction from parameter
        {
            transform.position += v;
        }
    }

}
