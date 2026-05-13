using System;

namespace KSY.Networks
{
    //AllowMultiple : 특성 중복 적용이 가능한가에 대한 여부
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PacketHandlerAttribute : Attribute
    {
        public Type PacketType => packetType;
        
        private readonly Type packetType;

        public PacketHandlerAttribute(Type packetType)
        {
            this.packetType = packetType;
        }
    }
}

