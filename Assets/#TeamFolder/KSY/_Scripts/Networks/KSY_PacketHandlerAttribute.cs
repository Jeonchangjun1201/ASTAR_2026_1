using System;
using UnityEngine;

namespace KSY.Networks
{
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

