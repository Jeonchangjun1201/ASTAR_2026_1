using System.Linq;
using UnityEngine;

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

        public event Action OnMiniGameEndEvent;

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
        }

        public void OutPlayer(Player player, int index)
        {
            CurrentPlayer--;
            PlayerList[index] = null;
            player.OnOutPlayerEvent -= OutPlayer;
            player.DelPlayer();
        }

        public void GameEnd()
        {
            PlayerAllDelEvent();

            List<int> winPlayerIndex = new List<int>();

            for (int i = 0; i < PlayerList.Length; i++)
            {
                if (PlayerList[i] != null)
                {
                    winPlayerIndex.Append(i);
                }
            }

            foreach (int playerIndex in winPlayerIndex)
            {
                Debug.Log($"Player {playerIndex}, Win.");
            }
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
