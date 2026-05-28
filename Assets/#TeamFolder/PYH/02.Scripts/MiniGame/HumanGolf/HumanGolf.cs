using System.Linq;
using _TeamFolder.PYH._02.Scripts.Player;
using _TeamFolder.PYH._02.Scripts.MiniGame;
using UnityEngine;
using UnityEngine.Events;

namespace _TeamFolder.PYH._02.Scripts.MiniGame.HumanGolf
{
    public class HumanGolf : AbstractMiniGame, IMiniGame
    {
        private bool _init;

        [field:SerializeField] public AbstractMiniGameModule[] ModuleList { get; private set; }
        public int MaxPlayer { get; private set; }
        public int CurrentPlayer { get; private set; }
        [field:SerializeField] public UnityEvent OnMiniGameEndEvent { get; private set; }

        public void Initialize()
        {
            if (_init) return;
            _init = true;

            ModuleList = FindObjectsOfType<HumanGolfModule>().ToArray();

            for (int i = 0; i < ModuleList.Length; i++)
            {
                HumanGolfModule module = ModuleList[i] as HumanGolfModule;

                module.Index = i;
                module.OnOutPlayerEvent += OutPlayer;
            }

            CurrentPlayer = ModuleList.Length;
        }

        public void OutPlayer(HumanGolfModule module, int index)
        {
            CurrentPlayer--;
            module.OnOutPlayerEvent -= OutPlayer;
            module.DelPlayer();

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
                HumanGolfModule player = module as HumanGolfModule;
                
                player.OnOutPlayerEvent -= OutPlayer;
            }
        }
    }
}
