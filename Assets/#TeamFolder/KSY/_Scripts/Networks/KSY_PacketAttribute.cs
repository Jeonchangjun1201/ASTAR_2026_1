using System;

namespace KSY.Networks
{
    //AttributeUsage : Specifies the usage of another attribute class, This class cannot be inherited
    //AllowMultiple : 하나의 대상에 동일한 Attribute를 여러 개 붙일 수 있는지 여부
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

