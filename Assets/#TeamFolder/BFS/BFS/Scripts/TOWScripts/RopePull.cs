using System;
using UnityEngine;
namespace BFS
{
    public class RopePull : AbstractTeamTOW
    {
        public override void Initialize(PlayerTeamTOW team, PlayerTOW player)
        {
            base.Initialize(team, player);
        }
        public override void ReceiveScore(ITeamTOW team, int score)
        {
            base.ReceiveScore(team, score);
        }
    }

}

