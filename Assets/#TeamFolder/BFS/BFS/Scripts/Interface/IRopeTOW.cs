using System.Collections.Generic;
using UnityEngine;
namespace GDH
{
    public interface IRopeTOW
    {
        void InitializeTeam(ITeamTOW team);
        void DeclareGoal(ITeamTOW team);
        void MoveRope(Vector3 vt);
        void GetInput(Vector2 vt, ITeamTOW team);
        void GiveScore(ITeamTOW team, bool IsCorrect);
    }

}

