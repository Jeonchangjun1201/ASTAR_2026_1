using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BFS
{
    public class TOWGameManager : MonoBehaviour
    {
        [SerializeField] private TOWKeyQTEManager qteManager;
        private AbstractTeamTOW[] playerList;
        private RopeTOW _rope;
        private TOWScoreManager _scoreManager;

        private void Awake()
        {
            playerList = GetComponentsInChildren<AbstractTeamTOW>();
            int cnt = 0;
            foreach (RopePull rp in playerList)
            {
                rp.Initialize(cnt++ % 2 == 0 ? PlayerTeamTOW.TEAMONE : PlayerTeamTOW.TEAMTWO, rp.GetComponentInParent<PlayerTOW>());
            }
            _scoreManager = new TOWScoreManager(playerList);
            _rope = GetComponentInChildren<RopeTOW>();
            qteManager.Initialize(_rope, playerList, _scoreManager);
        }
        private void Update()                                           // TEMPORARY; FOR DEBUGGING
        {
            if(Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Debug.Log($"{_scoreManager.CheckTeamScore(1)} - TeamOne");
            }
            if(Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log($"{_scoreManager.CheckTeamScore(2)} - TeamTwo");
            }
        }
        private void OnDestroy()
        {
            _scoreManager.OnDestroyThen();
        }
    }

}

