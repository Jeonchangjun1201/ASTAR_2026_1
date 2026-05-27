using System;
using UnityEngine;
using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;

namespace KSY.Shared.UI
{
    public class UIMiniGameRoulette : MonoBehaviour
    {
        [SerializeField] public KSY_PlayerBoxUI[] playerBoxUis = new KSY_PlayerBoxUI[4];
        public event Action<MiniGameDataSO> OnRouletteSpinStopping;
        public void RouletteUI(MiniGameDataSO miniGameData)
        {
            // RouletteUI ∑Í∑ø µπ∏Æ±‚
            AStarEventBus.Subscribe<RandomizerMiniGameInitEvent>(Initialize);

            // ∑Í∑ø ¥Ÿ µπ∏Æ∞Ì »£√‚«“ ∞Õ.
            OnRouletteSpinStopping?.Invoke(miniGameData);
        }
    }
}

