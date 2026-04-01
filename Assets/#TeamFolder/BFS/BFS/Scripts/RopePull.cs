using System;
using UnityEngine;
namespace GDH
{
    public class RopePull : AbstractTeamTOW
    {
        public override void Initialize(PlayerTeamTOW team, IRopeTOW rope)
        {
            base.Initialize(team, rope);
        }
        public override void ReceiveScore(int score)
        {
            base.ReceiveScore(score);
        }
        public override void SendInput(IRopeTOW rope, Vector2 input)
        {
            base.SendInput(rope, input);
        }
    }

}

