using System.Collections.Generic;
using UnityEngine;
namespace BFS
{
    public class RopeTOW : MonoBehaviour, IRopeTOW
    {

        private List<ITeamTOW> _teamList = new List<ITeamTOW>();
        public void MoveRope(Vector3 v)
        {
            transform.position += v;
        }
    }

}
