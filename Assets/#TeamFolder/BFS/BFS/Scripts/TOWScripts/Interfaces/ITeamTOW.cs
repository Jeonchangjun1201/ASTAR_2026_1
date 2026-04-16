using System;
using UnityEngine;
namespace BFS
{
    public interface ITeamTOW
    {
        string ObjectName { get; }                                //TEMPORARY; FOR DEBUGGING
        PlayerTeamTOW Team { get; }
        IRopeTOW Rope { get; }
        void Initialize(PlayerTeamTOW team, PlayerTOW player);
        void ReceiveScore(ITeamTOW team, int score);
    }

}
