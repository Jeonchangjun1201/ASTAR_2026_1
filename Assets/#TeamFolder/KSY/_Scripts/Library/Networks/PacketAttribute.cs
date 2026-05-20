using System;

namespace KSY.Networks
{
    //Attribute used to reduce packet memory

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PacketAttribute : Attribute
    {
        public ushort PacketID => packetID;
        
        private readonly ushort packetID;

        public PacketAttribute(ushort packetID)
        {
            this.packetID = packetID;
        }
    }
}

