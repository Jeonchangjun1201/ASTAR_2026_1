using MemoryPack;
using UnityEngine;

[MemoryPackable]
public partial class PlayerDataDTO
{
    public int Id { get; private set; }
    public int WinCount { get; private set; }
    public Color TeamColor { get; private set; }
    public PlayerDataDTO(int id, int winCount, Color teamColor)
    {
        this.Id = id;
        this.WinCount = winCount;
        this.TeamColor = teamColor;
    }
}