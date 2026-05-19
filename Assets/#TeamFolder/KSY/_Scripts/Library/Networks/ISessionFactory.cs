using System.Net.Sockets;

namespace KSY.Networks
{
    public interface ISessionFactory 
    {
        Session Create(NetworkObject networkObject, Socket connectedSocket);
    }
}

