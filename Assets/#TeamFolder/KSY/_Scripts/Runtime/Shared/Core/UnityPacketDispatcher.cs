using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using KSY.Networks;
using UnityEngine;

namespace KSY.Shared
{
    //Packet이 올바른 Handler로 가도록 도와주는 class
    public class UnityPacketDispatcher : MonoBehaviour, IPacketDispatcher
    {
        private readonly ConcurrentQueue<(Session, IPacket)> packetQueue = new ConcurrentQueue<(Session, IPacket)>();

        private bool isProcessing = false;
        private Lazy<PacketHandlerFactory> packetHandlerFactory = null;
        
        public void Initialize(IDIContainer diContainer)
        {
            isProcessing = false;
            packetHandlerFactory = new Lazy<PacketHandlerFactory>(() => diContainer.GetInstance<PacketHandlerFactory>());
        }

        private void Update()
        {
            if(isProcessing)
                return;

            bool isPacketQueueEmpty = packetQueue.Count <= 0;
            if(isPacketQueueEmpty)
                return;

            FlushQueueAsync().Forget();
        }

        private async UniTask FlushQueueAsync()
        {
            if(isProcessing == true)
                return;

            isProcessing = true;

            try
            {
                while(packetQueue.TryDequeue(out (Session session, IPacket packet) packetContext))
                {
                    try
                    {
                        Type packetType = packetContext.packet.GetType();

                        IPacketHandlerBase packetHandler = packetHandlerFactory?.Value.Create(packetType);
                        if(packetHandler != null)
                            await packetHandler.HandlePacket(packetContext.session, packetContext.packet);
                    }
                    catch (Exception err)
                    {
                        Debug.LogError(err);
                    }
                }
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
            finally
            {
                isProcessing = false;
            }
        }

        public ValueTask Dispatch(Session session, IPacket packet)
        {
            packetQueue.Enqueue((session, packet));
            return new ValueTask();
        }
    }
}

