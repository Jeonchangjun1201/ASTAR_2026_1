using System;
using UnityEngine;
namespace GDH
{
    public interface ITeamTOW
    {
        PlayerTeamTOW Team { get; }
        IRopeTOW Rope { get; }

        void Initialize(PlayerTeamTOW team, IRopeTOW rope);
        void SendInput(IRopeTOW rope, Vector2 input);
        void ReceiveScore(int score);
    }

}
