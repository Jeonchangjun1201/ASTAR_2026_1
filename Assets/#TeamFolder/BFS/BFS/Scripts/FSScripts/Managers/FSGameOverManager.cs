using System;
using System.Collections.Generic;

namespace BFS
{

    public class FSGameOverManager
    {
        private int _aliveCount;
        private bool _finalCountActivated = false;
        public event Action OnFinalCountdown;
        public event Action<string> OnGameEnd;

        public FSGameOverManager(List<FSPlayer> playerList)                                              // Constructor // 생성자, 영어 못 읽을 거 같아서 앞으로 한글로 많이 쓸게요
        {   
            foreach (FSPlayer player in playerList)
            {
                player.OnOut += CountOuts;
                _aliveCount++;
            }
        }
        public void GameOver(List<FSPlayer> playerList)                                                  // GameOver method that invokes action to return string
        {                                                                                                // 액션 함수를 호출하여 string값을 게임 매니저에 반환해서 게임오버 텍스트를 띄우는 메서드
            if (_aliveCount == 0)                                                                        // No Survivor // 생존자가 없을 때
            {
                string s = "No one survived :c";
                OnGameEnd?.Invoke(s);
            }
            else if(_aliveCount == 1)                                                                    // Last Survivor // 생존자가 1명일 때
            {
                FSPlayer Lastplayer = null;
                foreach (FSPlayer player in playerList)
                {
                    if (player.IsOut == false)
                        Lastplayer = player;
                }
                string s = Lastplayer.gameObject.name + " WON!!!";
                OnGameEnd?.Invoke(s);
            }
            else                                                                                         // Else // 이 외; 생존자가 여러 명(마지막 스테이지까지 통과)일 때
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

        public bool UpdateFinalCountdown()                                                                // Checks player every frame // 매 프레임마다 확인해서 생존자가 1명밖에 없을 때 강제로 게임 종료
        {
            if (_aliveCount <= 1 & !_finalCountActivated)
            {
                OnFinalCountdown?.Invoke();
                _finalCountActivated = true;
                return true;
            }
            return false;
        }
        public void DestroyThenPlay(List<FSPlayer> playerList)                                            // Unsub purpose // 구독해제 하기 위해 존재함
        {
            foreach (FSPlayer player in playerList)
                player.OnOut -= CountOuts;
        }
        private void CountOuts() => _aliveCount--;

    }
}
