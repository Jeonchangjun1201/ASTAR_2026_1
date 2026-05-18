using Cysharp.Threading.Tasks;
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
            ClientInstance.GameClient = this;

            session = new Session();
            session.OnOpenedEvent += async session =>
            {
                await UniTask.Yield();
                session.SendAsync(new C2S_EnterGameRequestPacket());
            };

            UnityPacketDispatcher unityPacketDispatcher = gameManager.gameObject.AddComponent<UnityPacketDispatcher>();
            client = new ClientBuilder
        }
    }
}


