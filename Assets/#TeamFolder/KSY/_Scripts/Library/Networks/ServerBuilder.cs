using System;

namespace KSY.Networks
{
    public class ServerBuilder : NetworkObjectBuilder<Server>
    {
        private IPacketDispatcher _roomPacketDispatcher;
        //RoomWorker의 수
        private int _workerCount =  -1;
        //각 RoomWorker가 담을 수 있는 최대 패킷량
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

            RoomManager instance = new RoomManager(_roomPacketDispatcher, diContainer, _workerCount, _capacityPerWorker);
            AddSingleton((IPacketDispatcher)instance);
            AddSingleton((IRoomManager)instance);
            return new Server(this);
        }
    }
}
