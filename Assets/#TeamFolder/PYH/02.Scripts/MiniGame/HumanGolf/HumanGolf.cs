using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace _TeamFolder.PYH._02.Scripts.MiniGame.HumanGolf
{
    public class HumanGolf : AbstractMiniGame, IMiniGame
    {
        private bool _init;

        [field:SerializeField] public Player.Player[] PlayerList { get; private set; }

        public int MaxPlayer { get; private set; }
        public int CurrentPlayer { get; private set; }
        public UnityEvent OnMiniGameEndEvent { get; }

        public void Initialize()
        {
            if (_init) return;
            _init = true;

            PlayerList = FindObjectsOfType<Player.Player>().ToArray<Player.Player>(); // Temporary, Load Player

            for (int i = 0; i < PlayerList.Length; i++)
            {
                Player.Player player = PlayerList[i];

                player.index = i;
                player.OnOutPlayerEvent += OutPlayer;
            }

            CurrentPlayer = PlayerList.Length;
        }

        public void OutPlayer(Player.Player player, int index)
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

            for (int i = 0; i < PlayerList.Length; i++)
            {
                if (PlayerList[i].gameObject.activeSelf)
                {
                    Debug.Log($"Player {PlayerList[i].index}, Win.");
                }
            }
            
            OnMiniGameEndEvent?.Invoke();
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
