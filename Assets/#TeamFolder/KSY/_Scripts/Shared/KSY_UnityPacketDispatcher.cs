using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using KSY.Networks;
using UnityEngine;

namespace KSY.Shared
{
    public class KSY_UnityPacketDispatcher : MonoBehaviour, KSY_IPacketDispatcher
    {
        private readonly ConcurrentQueue<(KSY_Session, KSY_IPacket)> packetQueue = new ConcurrentQueue<(KSY_Session, KSY_IPacket)>();

        private bool isProcessing = false;
        private Lazy<KSY_PacketHandlerFactory> packetHandlerFactory = null;
        
        public void Initialize(KSY_IDIContainer diContainer)
        {
            isProcessing = false;
            packetHandlerFactory = new Lazy<KSY_PacketHandlerFactory>(() => diContainer.GetInstance<KSY_PacketHandlerFactory>());
        }

        private void Update()
        {
            if(isProcessing)
                return;

            if(packetQueue.Count <= 0)
                return;

            FlushQueueAsync().Forget();
        }

        private async UniTask FlushQueueAsync()
        {
            
        }

    }
}

