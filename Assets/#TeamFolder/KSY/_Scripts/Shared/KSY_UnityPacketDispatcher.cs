using System;
using System.Collections.Concurrent;
using KSY.Networks;
using UnityEngine;

namespace KSY.Shared
{
    public class KSY_UnityPacketDispatcher : MonoBehaviour, KSY_IPacketDispatcher
    {
        private readonly ConcurrentQueue<(KSY_Session, KSY_IPacket)> packetQueue = new ConcurrentQueue<(KSY_Session, KSY_IPacket)>();

        private bool isProcessing = false;
        private Lazy<KSY_PacketHandlerFactory>
        
    }
}

