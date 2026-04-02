using System.Collections.Generic;
using UnityEngine;
namespace GDH
{
    public class RopeTOW : MonoBehaviour, IRopeTOW
    {
        private char _inputShower;
        private Vector2 _goalVector;

        private List<ITeamTOW> _teamList = new List<ITeamTOW>();
        public void InitializeTeam(ITeamTOW team)
        {
            _teamList.Add(team);
            DeclareGoal(team);
        }
        public void DeclareGoal(ITeamTOW team)
        {
            _goalVector = RollInput();
            Debug.Log($"{_inputShower} - {team.Team}");
        }
        public void GetInput(Vector2 vt, ITeamTOW team)
        {
            if (vt == _goalVector)
            {
                GiveScore(team, true);
            }
            else
            {
                GiveScore(team, false);
            }
        }
        public void GiveScore(ITeamTOW team, bool IsCorrect)
        {
            Debug.Log(IsCorrect ? "<color=green>SUCCESS!</color>" : "<color=red>WRONG!</color>");
            team.ReceiveScore(IsCorrect ? 1 : -1);
            DeclareGoal(team);
            Vector3 moveDir;
            switch (team.Team)
            {
                case PlayerTeamTOW.TEAMONE:
                    moveDir = Vector3.left;
                    break;
                default:
                    moveDir = Vector3.right;
                    break;
            }
            if (!IsCorrect)
                moveDir *= -1;
            moveDir *= 0.2f;
            MoveRope(moveDir);
        }
        public void MoveRope(Vector3 v)
        {
            transform.position += v;
        }
        private Vector2 RollInput()
        {
            switch (Random.Range(0, 4))
            {
                case 0:
                    _inputShower = 'W';
                    _goalVector = Vector2.up;
                    return Vector2.up;
                case 1:
                    _inputShower = 'A';
                    _goalVector = Vector2.left;
                    return Vector2.left;
                case 2:
                    _inputShower = 'S';
                    _goalVector = Vector2.down;
                    return Vector2.down;
                default:
                    _inputShower = 'D';
                    _goalVector = Vector2.right;
                    return Vector2.right;
            }
        }
    }

}
