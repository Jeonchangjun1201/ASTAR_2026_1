using MemoryPack;
using UnityEngine;

namespace KSY.Shared
{
    [MemoryPackable]
    public partial class UnitDataDTO
    {
        public Vector2 Position { get; set; }
    }
}

