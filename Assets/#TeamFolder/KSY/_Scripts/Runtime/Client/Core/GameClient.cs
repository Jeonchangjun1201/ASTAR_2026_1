using BackEnd;
using Cysharp.Threading.Tasks;
using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Utility;
using System;

namespace KSY.Clients
{
    public class GameClient
    {
        private Session session = null;
        private Client client = null;

        public void Initialize(GameManager gameManager, DataTableManager dataTableManager)
        {
            GameInstance.PlayMode |= EPlayMode.Client;
            GameInstance.DataTableManager = dataTableManager;
            ClientInstance.GameClient = this;

            session = new Session();
            session.OnOpenedEvent += async session =>
            {
                await UniTask.Yield();
                session.SendAsync(new C2S_EnterRoomRequestPacket()
                {
                    PlayerName = Backend.UserNickName
                });
            };

            if(!gameManager.TryGetComponent(out UnityPacketDispatcher unityPacketDispatcher))
                unityPacketDispatcher = gameManager.gameObject.AddComponent<UnityPacketDispatcher>();
            client = new ClientBuilder(session, unityPacketDispatcher)
                .AddSingleton<GameClient>(this)
                .AddSingleton<GameManager>(gameManager)
                .AddSingleton<DataTableManager>(dataTableManager)
                .Build(typeof(GameClient).Assembly, typeof(GameManager).Assembly);
            
            unityPacketDispatcher.Initialize(client);
        }

        public void Connect(string host, int port, Action onConnected) => client.Connect(host, port, onConnected);
        public void Send(IPacket packet) => session.SendAsync(packet);
    }
}


