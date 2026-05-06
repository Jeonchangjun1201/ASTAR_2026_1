using KSY.Client;
using KSY.Networks;
using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class KSY_Session : MonoBehaviour
{
    private readonly object sendLocker;
    private KSY_SendQueue sendQueue;
    private SocketAsyncEventArgs sendArgs;
    private ReceiveBuffer receiveBuffer;

    private SocketAsyncEventArgs receiveArgs;
    private Socket connectedSocket;
    private KSY_PacketSerializer packetSerializer;
    private KSY_IPacketDispatcher packetDispatcher;

    private int isClosed = 1;
    public bool IsOpened
    {
        get
        {
            if (Volatile.Read(ref isClosed) == 0 && connectedSocket != null)
            {
                return connectedSocket.Connected;
            }

            return false;
        }
    }

    public event Action<KSY_Session> OnOpenedEvent;
    public event Action<KSY_Session> OnClosedEvent;
    public event Action<KSY_Session, Exception> OnErrorEvent;
    
    public KSY_Session()
    {
        sendLocker = new object();
    }

    public void Open(Socket connectedSocket, KSY_PacketSerializer packetSerializer, KSY_IPacketDispatcher packetDispatcher)
    {

    }
}
