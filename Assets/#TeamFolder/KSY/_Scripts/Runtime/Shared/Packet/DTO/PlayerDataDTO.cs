using MemoryPack;
using UnityEngine;

[MemoryPackable]
public partial class PlayerDataDTO
{
    public string Nickname { get; private set; }
    public int Id { get; private set; }
    public int WinCount { get; private set; }
    public Color TeamColor { get; private set; }
    public PlayerDataDTO(string nickname, int id, int winCount, Color teamColor)
    {
        this.Nickname = nickname;
        this.Id = id;
        this.WinCount = winCount;
        this.TeamColor = teamColor;
    }
}