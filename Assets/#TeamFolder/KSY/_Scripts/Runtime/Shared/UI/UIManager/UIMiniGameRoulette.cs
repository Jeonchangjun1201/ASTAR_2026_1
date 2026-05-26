using System;
using UnityEngine;

namespace KSY.Shared.UI
{
    public class UIMiniGameRoulette : MonoBehaviour
    {
        [SerializeField] public KSY_PlayerBoxUI[] playerBoxUis = new KSY_PlayerBoxUI[4];
        public event Action<MiniGameDataSO> OnRouletteSpinStopping;
        public void RouletteUI(MiniGameDataSO miniGameData)
        {
            // RouletteUI ∑Í∑ø µπ∏Æ±‚

            // ∑Í∑ø ¥Ÿ µπ∏Æ∞Ì »£√‚«“ ∞Õ.
            OnRouletteSpinStopping?.Invoke(miniGameData);
        }
    }
}

