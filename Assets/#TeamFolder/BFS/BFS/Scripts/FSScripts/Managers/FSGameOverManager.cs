using BFS;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace BFS
{

    public class FSGameOverManager
    {
        private int _aliveCount;
        private bool _finalCountActivated = false;
        public event Action OnFinalCountdown;
        public event Action<string> OnGameEnd;

        public FSGameOverManager(List<FSPlayer> playerList)
        {
            foreach (FSPlayer player in playerList)
            {
                player.OnOut += CountOuts;
                _aliveCount++;
            }
        }
        public void GameOver(List<FSPlayer> playerList)
        {
            if (_aliveCount == 0)
            {
                string s = "No one survived :c";
                OnGameEnd?.Invoke(s);
            }
            else if(_aliveCount == 1)
            {
                FSPlayer Lastplayer = null;
                foreach (FSPlayer player in playerList)
                {
                    if (player.IsOut == false)
                        Lastplayer = player;
                }
                string s = Lastplayer.GetComponentInParent<PlayerBFS>().gameObject.name + " WON!!!";
                OnGameEnd?.Invoke(s);
            }
            else
            {
                string s = null;
                int cnt = 0;
                foreach (FSPlayer p in playerList)
                {
                    if (p.IsOut) continue;
                    if (cnt > 0)
                        s += ", ";
                    s += p.GetComponentInParent<PlayerBFS>().gameObject.name;
                    cnt++;
                }
                s += " WON!!!!";
                OnGameEnd?.Invoke(s);
            }
        }

        public bool UpdateFinalCountdown()
        {
            if (_aliveCount <= 1 & !_finalCountActivated)
            {
                OnFinalCountdown?.Invoke();
                _finalCountActivated = true;
                return true;
            }
            return false;
        }
        public void DestroyThenPlay(List<FSPlayer> playerList)
        {
            foreach (FSPlayer player in playerList)
                player.OnOut -= CountOuts;
        }
        private void CountOuts() => _aliveCount--;

    }
}
