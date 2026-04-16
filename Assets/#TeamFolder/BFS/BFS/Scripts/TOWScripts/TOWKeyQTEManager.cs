using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BFS
{
    public class TOWKeyQTEManager : MonoBehaviour                                                                // Key minigame manager script
    {
        private IRopeTOW _rope;
        private List<AbstractTeamTOW> _teamList = new List<AbstractTeamTOW>();                                   // List that contains players, used for sub/unsubing GetInput method to OnInputPressed action 
        private Dictionary<ITeamTOW, Vector2> _goalDict = new Dictionary<ITeamTOW, Vector2>();                   // Dictionary that contains required key to press for each player(Key: ITeamTOW interface(player), Value: Vector2(Required input key to press))
        private TOWScoreManager _scoreManager;                                                                   // Score manager, this exists so that rope doesn't move when team's score is 0
        private char _inputShower;                                                                               // Char variable that shows which key to press
        private bool _isPenalty = false;                                                                         // Bool variable, used to detect if player should have penalty or not
        private float _penaltyTime = 2.5f;                                                                       // How long minigame is going to be disabled for player
        public void Initialize(IRopeTOW rope, AbstractTeamTOW[] playerList, TOWScoreManager scoreManager)        // Initialize
        {
            _rope = rope;
            foreach (AbstractTeamTOW t in playerList)                                                            // Subs to each player's OnInputPressed action
            {
                _teamList.Add(t);
                t.OnInputPressed += GetInput;
            }
            foreach (ITeamTOW t in playerList)                                                                   // Adds each player to dictionary, for input key management
            {
                _goalDict.Add(t, Vector2.zero);
                DeclareGoal(t);                                                                                  // Initiate the input key minigame
            }
            _scoreManager = scoreManager;
        }

        private void OnDestroy()
        {
            foreach (AbstractTeamTOW t in _teamList)                                                             // Unsub
            {
                t.OnInputPressed -= GetInput;
            }
        }
        private IEnumerator NextInputCoroutine(ITeamTOW team, bool val)                                          // Coroutine, will give penalty to a player if they messed up with the minigame
        {
            if (val)                                                                                             // If parameter(IsCorrect) is true, then give next required input
                DeclareGoal(team);
            else                                                                                                 // Else, make playeer do nothing for penalty time, then give next required input
            {
                _isPenalty = true;
                Debug.Log($"<color=red>You can't do anything for {_penaltyTime} seconds...</color>");            // TEMPORARY; for debugging, change this to UI when available
                yield return new WaitForSeconds(_penaltyTime);
                _isPenalty = false;
                DeclareGoal(team);
            }
        }
        public void GiveScore(ITeamTOW team, bool IsCorrect)                                                     // Method that decides whether player pressed correct key or not
        {
            Debug.Log(IsCorrect ? "<color=green>SUCCESS!</color>" : "<color=red>WRONG!</color>");                // TEMPORARY; for debugging, shows if its correct or wrong, should be changed to UI
            Vector3 moveDir;                                                                                     // Local variable, decides direction for the rope to move
            switch (team.Team)                                                                                   // The side for each team are opposite. So modify the default movement direction for rope based on the team
            {
                case PlayerTeamTOW.TEAMONE:
                    moveDir = Vector3.left;
                    break;
                default:
                    moveDir = Vector3.right;
                    break;
            }
            moveDir *= 0.2f;                                                                                     // Modifying movement distance
            _rope.MoveRope(IsCorrect ? moveDir :                                                                 // If player pressed correct key, then move it to default direction
                _scoreManager.scoreBoard[team.Team] > 0 ? (moveDir * -1) : (moveDir * 0));                       // Else, move rope to opposite direction unless corresponding team has score of 0
            team.ReceiveScore(team, IsCorrect ? 1 : -1);                                                         // Give team a score; 1 if they pressed correct key, -1 else
            StartCoroutine(NextInputCoroutine(team, IsCorrect));                                                 // Coroutine, gives player a penalty if they pressed wrong key.
        }
        public Vector2 DeclareGoal(ITeamTOW team)                                                                // Sets the required input key for each player
        {
            Vector2 goal = RollInput(team);                                                                      // Declare local variable, then give it Shuffled movement input key
            Debug.Log($"{_inputShower} - {team.Team} | {team.ObjectName}");                                      // TEMPORARY; debugging, shows required input key, team name, and player name
            return goal;                                                                                         // returns required input key
        }
        public void GetInput(ITeamTOW team, Vector2 vt)                                                          // Method that subscribes to action, receives ITeamTOW and Vector2(each are player and input)
        {
            if (_isPenalty)                                                                                      // Ignore if player is in penalty state
                return;
            if (vt == _goalDict[team])                                                                           // If input equals to required key given to each player
            {
                GiveScore(team, true);                                                                           // Call GiveScore, IsCorrect = true
            }
            else                                                                                                 // Else then call GiveScore, IsCorrect = false
            {
                GiveScore(team, false);
            }
        }
        private Vector2 RollInput(ITeamTOW team)                                                                 // Method to shuffle required input key, then return it
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
