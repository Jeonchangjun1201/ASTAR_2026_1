using System.Linq;
using _TeamFolder.PYH._02.Scripts.Player;
using UnityEngine;
using UnityEngine.Events;

namespace _TeamFolder.PYH._02.Scripts.MiniGame.PassTheBomb
{
    public class PassTheBomb : AbstractMiniGame, IMiniGame
    {
        private bool _init;

        public AbstractMiniGameModule[] ModuleList { get; private set; }
        public int MaxPlayer { get; private set; }
        public int CurrentPlayer { get; private set; }
        [field: SerializeField] public UnityEvent OnMiniGameEndEvent { get; private set; }
        [SerializeField] private Bomb currentBomb;
        
        public void Initialize()
        {
            if (_init) return;
            _init = true;

            Debug.Assert(currentBomb != null, "currentBomb is null");
            
            ModuleList = FindObjectsOfType<PassTheBombModule>().ToArray(); // Temporary, Load Player

            for (int i = 0; i < ModuleList.Length; i++)
            {
                PassTheBombModule module = ModuleList[i] as PassTheBombModule;

                module.Index = i;
                module.onExplosionEvent += OutPlayer;
            }

            CurrentPlayer = ModuleList.Length;
            currentBomb.StartBomb(RandomPlayer());
        }
        
        public void OutPlayer(PassTheBombModule player, int index)
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

            for (int i = 0; i < ModuleList.Length; i++)
            {
                if (ModuleList[i].gameObject.activeSelf)
                {
                    Debug.Log($"Player {ModuleList[i].Index}, Win.");
                }
            }
            
            OnMiniGameEndEvent?.Invoke();
        }
        
        private void PlayerAllDelEvent()
        {
            foreach (var module in ModuleList)
            {
                PassTheBombModule player = module as PassTheBombModule;
                player.onExplosionEvent -= OutPlayer;
            }
        }

        private PassTheBombModule RandomPlayer()
        {
            PassTheBombModule player = ModuleList[Random.Range(0, ModuleList.Length)] as PassTheBombModule;

            return !player.gameObject.activeSelf ? RandomPlayer() : player;
        }
    }
}
