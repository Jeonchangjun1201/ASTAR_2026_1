using System;
using KSY.Servers;

namespace KSY.Networks
{
    public class ServerBuilder : NetworkObjectBuilder<Server>
    {
        private IPacketDispatcher _roomPacketDispatcher;
        private int _workerCount =  -1;
        private int _capacityPerWorker = 4096;

        public ServerBuilder(ISessionFactory sessionFactory, IPacketDispatcher roomPacketDispatcher = null)
        {
            this._roomPacketDispatcher = roomPacketDispatcher;
            AddSingleton(sessionFactory);
        }

        public ServerBuilder SetWorkerCount(int workerCount)
        {
            this._workerCount = workerCount;
            return this;
        }

        public ServerBuilder SetCapacityPerWorker(int capacityPerWorker)
        {
            this._capacityPerWorker = capacityPerWorker;
            return this;
        }

        protected override Server OnBuild()
        {
            bool workerIsEmpty = _workerCount <= 0;
            if (workerIsEmpty)
                _workerCount = Environment.ProcessorCount;

            RoomManager instance = new RoomManager()
        }
    }
}
