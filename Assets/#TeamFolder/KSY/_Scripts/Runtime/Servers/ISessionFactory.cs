using KSY.Networks;
using System.Net.Sockets;
using UnityEngine;

namespace KSY.Servers
{
    public interface ISessionFactory 
    {
        Session Create(NetworkObject networkObject, Socket connectedSocket);
    }
}

