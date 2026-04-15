using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BFS
{
    public class TOWKeyQTEManager : MonoBehaviour
    {
        private IRopeTOW _rope;
        private List<AbstractTeamTOW> _teamList = new List<AbstractTeamTOW>();
        private Dictionary<ITeamTOW, Vector2> _goalDict = new Dictionary<ITeamTOW, Vector2>();
        private TOWScoreManager _scoreManager;
        private char _inputShower;
        private bool _isPenalty = false;
        private float _penaltyTime = 2.5f;
        public void Initialize(IRopeTOW rope, AbstractTeamTOW[] playerList, TOWScoreManager scoreManager)
        {
            _rope = rope;
            foreach (AbstractTeamTOW t in playerList)
            {
                _teamList.Add(t);
                t.OnInputPressed += GetInput;
            }
            foreach (ITeamTOW t in playerList)
            {
                _goalDict.Add(t, Vector2.zero);
                DeclareGoal(t);
            }
            _scoreManager = scoreManager;
        }

        private void OnDestroy()
        {
            foreach (AbstractTeamTOW t in _teamList)
            {
                t.OnInputPressed -= GetInput;
            }
        }
        private IEnumerator NextInputCoroutine(ITeamTOW team, bool val)
        {
            if (val)
                DeclareGoal(team);
            else
            {
                _isPenalty = true;
                Debug.Log($"<color=red>You can't do anything for {_penaltyTime} seconds...</color>");
                yield return new WaitForSeconds(_penaltyTime);
                _isPenalty = false;
                DeclareGoal(team);
            }
        }
        public void GiveScore(ITeamTOW team, bool IsCorrect)
        {
            Debug.Log(IsCorrect ? "<color=green>SUCCESS!</color>" : "<color=red>WRONG!</color>");
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
            moveDir *= 0.2f;
            _rope.MoveRope(IsCorrect ? moveDir :
                _scoreManager.scoreBoard[team.Team] > 0 ? (moveDir * -1) : (moveDir * 0));
            team.ReceiveScore(team, IsCorrect ? 1 : -1);
            StartCoroutine(NextInputCoroutine(team, IsCorrect));
        }
        public Vector2 DeclareGoal(ITeamTOW team)
        {
            Vector2 goal = RollInput(team);
            Debug.Log($"{_inputShower} - {team.Team} | {team.ObjectName}");
            return goal;
        }
        public void GetInput(ITeamTOW team, Vector2 vt)
        {
            if (_isPenalty)
                return;
            if (vt == _goalDict[team])
            {
                GiveScore(team, true);
            }
            else
            {
                GiveScore(team, false);
            }
        }
        private Vector2 RollInput(ITeamTOW team)
        {
            switch (Random.Range(0, 4))
            {
                case 0:
                    _inputShower = 'W';
                    _goalDict[team] = Vector2.up;
                    return Vector2.up;
                case 1:
                    _inputShower = 'A';
                    _goalDict[team] = Vector2.left;
                    return Vector2.left;
                case 2:
                    _inputShower = 'S';
                    _goalDict[team] = Vector2.down;
                    return Vector2.down;
                default:
                    _inputShower = 'D';
                    _goalDict[team] = Vector2.right;
                    return Vector2.right;
            }
        }
    }

}
