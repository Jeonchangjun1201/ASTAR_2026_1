using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BFS
{
    public class TOWGameManager : MonoBehaviour                                                                                         // Tug Of War manager script
    {
        [SerializeField] private TOWKeyQTEManager qteManager;
        [SerializeField] private float gameTime;
        private AbstractTeamTOW[] playerList;                                                                                           // Array that contains players(absctract class)
        private RopeTOW _rope;
        private TOWScoreManager _scoreManager;

        private void Awake()
        {
            playerList = GetComponentsInChildren<AbstractTeamTOW>();                                                                    // Collect players attached to game manager
            int cnt = 0;
            foreach (RopePull rp in playerList)                                                                                         // Initialize each player; apply team, and player script attached to them 
            {
                rp.Initialize(cnt++ % 2 == 0 ? PlayerTeamTOW.TEAMONE : PlayerTeamTOW.TEAMTWO, rp.GetComponentInParent<PlayerTOW>());
            }
            _scoreManager = new TOWScoreManager(playerList);                                                                            // Constructor; sends playerList to ScoreManager then instantiates
            _rope = GetComponentInChildren<RopeTOW>();
            qteManager.Initialize(_rope, playerList, _scoreManager);                                                                    // Initialize Key minigame manager

        }
        private void Update()                                                                                                           // TEMPORARY; FOR DEBUGGING
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
            _scoreManager.OnDestroyThen();                                                                                              // On destroy then calls it so score manager can unsub
        }
    }

}

