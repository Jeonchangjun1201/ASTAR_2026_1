using KSY.Networks;
using KSY.Shared;

namespace KSY.Clients
{
    public class GameClient
    {
        private Session session = null;
        private Client client = null;

        public void Initialize(GameManager gameManager, DataTableManager dataTableManager)
        {
            GameInstance.PlayMode = EPlayMode.Client;
            GameInstance.DataTableManager = dataTableManager;
            ClientInstance
        }
    }
}


