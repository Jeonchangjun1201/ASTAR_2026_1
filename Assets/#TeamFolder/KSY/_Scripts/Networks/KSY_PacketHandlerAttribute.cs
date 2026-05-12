using System;

namespace KSY.Networks
{
    //AllowMultiple : 특성 중복 적용이 가능한가에 대한 여부
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class KSY_PacketHandlerAttribute : Attribute
    {
        private readonly Type packetType;

        public Type PacketType => packetType;

        public KSY_PacketHandlerAttribute(Type packetType)
        {
            this.packetType = packetType;
        }
    }
}

