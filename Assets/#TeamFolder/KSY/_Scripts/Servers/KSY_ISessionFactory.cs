using KSY.Networks;
using System.Net.Sockets;
using UnityEngine;

namespace KSY.Servers
{
    public interface KSY_ISessionFactory 
    {
        KSY_Session Create(KSY_NetworkObject networkObject, Socket connectedSocket);
    }
}

