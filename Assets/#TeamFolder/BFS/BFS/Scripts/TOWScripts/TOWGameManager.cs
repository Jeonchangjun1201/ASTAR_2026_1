using System.Collections.Generic;
using UnityEngine;
namespace GDH
{
    public class TOWGameManager : MonoBehaviour
    {
        [field: SerializeField] private List<RopePull> Team;
        [field: SerializeField] private RopeTOW Rope;

        private void Awake()
        {
            int cnt = 0;
            foreach (RopePull rp in Team)
            {
                rp.Initialize(cnt++ % 2 == 0 ? PlayerTeamTOW.TEAMONE : PlayerTeamTOW.TEAMTWO, Rope);
            }
        }
    }

}

