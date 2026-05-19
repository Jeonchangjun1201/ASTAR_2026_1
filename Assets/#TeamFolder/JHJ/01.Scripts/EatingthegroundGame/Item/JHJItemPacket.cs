using JHJ.Scripts.Test.TestPlayer;
using UnityEngine;

public class JHJItemPacket : MonoBehaviour
{
    public enum ItemType { MoveSpeed, BrushSize, Knockback }

    [System.Serializable]
    public struct ItemConsumePacket
    {
        public PlayerIndex TargetPlayerIndex; // 먹은 사람
        public ItemType ConsumedItemType;     // 먹은 아이템
        public Vector3 ItemPosition;          // 아이템 위치 (넉백 계산용)
    }
}
