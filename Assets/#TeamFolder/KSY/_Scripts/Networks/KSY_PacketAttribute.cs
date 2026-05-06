using System;

namespace KSY.Networks
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class KSY_PacketAttribute : Attribute
    {
        public ushort PacketID => packetID;
        
        private readonly ushort packetID;

        public KSY_PacketAttribute(ushort packetID)
        {
            this.packetID = packetID;
        }
    }
}

