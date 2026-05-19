namespace KSY.Networks
{
    public class ClientBuilder : NetworkObjectBuilder<Client>
    {
        public ClientBuilder(Session session, IPacketDispatcher packetDispatcher)
        {
            AddSingleton(session);
            AddSingleton(packetDispatcher);
        }

        protected override Client OnBuild()
        {
            return new Client(this);
        }
    }
}
