using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace PYH.MiniGame
{
    using PYH.Player;
    using System;
    using System.Collections.Generic;

    public class HumanGolf : MonoBehaviour, IMiniGame
    {
        private bool _init;

        [field:SerializeField] public Player[] PlayerList { get; private set; }

        public int MaxPlayer { get; private set; }
        public int CurrentPlayer { get; private set; }
        
        public UnityEvent onMiniGameEndEvent;

        public void Initialize()
        {
            if (_init) return;
            _init = true;

            PlayerList = FindObjectsOfType<Player>().ToArray<Player>(); // Temporary, Load Player

            for (int i = 0; i < PlayerList.Length; i++)
            {
                Player player = PlayerList[i];

                player.index = i;
                player.OnOutPlayerEvent += OutPlayer;
            }

            CurrentPlayer = PlayerList.Length;
        }

        public void OutPlayer(Player player, int index)
        {
            Debug.Log($"{player.gameObject.name} �÷��̾�, �̺�Ʈ ����");

            CurrentPlayer--;
            player.OnOutPlayerEvent -= OutPlayer;
            player.DelPlayer();

            if (CurrentPlayer == 1)
            {
                Debug.Log($"GAME SET!");
                GameEnd();
            }
        }

        public void GameEnd()
        {
            PlayerAllDelEvent();

            if (CurrentPlayer == MaxPlayer)
            {
                Debug.Log("All Player Def.");
            }

            List<int> winPlayerIndex = new List<int>();

            for (int i = 0; i < PlayerList.Length; i++)
            {
                if (PlayerList[i].gameObject.activeSelf)
                {
                    Debug.Log($"Player {PlayerList[i].index}, Win.");
                }
            }
            
            onMiniGameEndEvent?.Invoke();
        }

        private void PlayerAllDelEvent()
        {
            foreach (var player in PlayerList)
            {
                player.OnOutPlayerEvent -= OutPlayer;
            }
        }
    }
}
