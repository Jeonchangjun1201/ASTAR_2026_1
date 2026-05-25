using UnityEngine;

namespace KSY.Clients
{
    public class MiniGame
    {
        public MiniGame(MiniGameDataSO data)
        {
            this.Data = data;
        }

        public MiniGameDataSO Data { get; private set; }
    }
}