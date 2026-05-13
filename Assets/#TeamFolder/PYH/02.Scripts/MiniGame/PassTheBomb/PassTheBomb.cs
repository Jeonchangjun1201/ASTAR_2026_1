using System.Linq;
using PYH.MiniGame;
using PYH.Player;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGame.PassTheBomb
{
    public class PassTheBomb : AbstractMiniGame, IMiniGame
    {
        private bool _init;
        
        public Player[] PlayerList { get; private set; }
        public int MaxPlayer { get; private set; }
        public int CurrentPlayer { get; private set; }
        [field: SerializeField] public UnityEvent OnMiniGameEndEvent { get; private set; }
        [SerializeField] private Bomb currentBomb;
        
        public void Initialize()
        {
            if (_init) return;
            _init = true;

            Debug.Assert(currentBomb != null, "currentBomb is null");
            
            PlayerList = FindObjectsOfType<Player>().ToArray<Player>(); // Temporary, Load Player

            for (int i = 0; i < PlayerList.Length; i++)
            {
                Player player = PlayerList[i];

                player.index = i;
                player.onExplosionEvent += OutPlayer;
            }

            CurrentPlayer = PlayerList.Length;
            currentBomb.StartBomb(RandomPlayer());
        }
        
        public void OutPlayer(Player player, int index)
        {
            CurrentPlayer--;
            player.onExplosionEvent -= OutPlayer;
            player.DelPlayer();

            if (CurrentPlayer == 1)
            {
                Debug.Log($"GAME SET!");
                GameEnd();
            }
            else
            {
                currentBomb.StartBomb(RandomPlayer());
                currentBomb.StartTimer();
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
                player.onExplosionEvent -= OutPlayer;
            }
        }

        private Player RandomPlayer()
        {
            Player player = PlayerList[Random.Range(0, PlayerList.Length)];

            return !player.gameObject.activeSelf ? RandomPlayer() : player;
        }
    }
}
